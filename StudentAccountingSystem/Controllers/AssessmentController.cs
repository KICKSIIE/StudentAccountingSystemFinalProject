using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using StudentAccountingSystem.Models;

namespace StudentAccountingSystem.Controllers
{
    public class AssessmentController : Controller
    {
        private readonly string _conn = ConfigurationManager
            .ConnectionStrings["StudentAccountingDB"].ConnectionString;

        // Simple check if user is admin
        private bool IsAdmin()
        {
            if (Session["Role"] != null && Session["Role"].ToString() == "Admin")
                return true;
            return false;
        }

        // INDEX: Show the assessment page
        public ActionResult Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");
            return View();
        }

        // Get all students with their assessment status - NO [HttpPost]
        public ActionResult GetStudentAssessmentStatus()
        {
            var data = new List<object>();
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
            SELECT  s.StudentID,
                    s.FirstName + ' ' + s.LastName AS StudentName,
                    c.CourseCode,
                    s.YearLevel,
                    s.Semester,
                    s.SchoolYear,
                    s.Status,
                    -- Active assessments for current student sem
                    (SELECT COUNT(*) FROM Assessments a
                     WHERE a.StudentID  = s.StudentID
                       AND a.SchoolYear = s.SchoolYear
                       AND a.Semester   = s.Semester
                       AND a.Status    != 'Refunded') AS ActiveCount,
                    -- Refunded assessments for current student sem
                    (SELECT COUNT(*) FROM Assessments a
                     WHERE a.StudentID  = s.StudentID
                       AND a.SchoolYear = s.SchoolYear
                       AND a.Semester   = s.Semester
                       AND a.Status     = 'Refunded') AS RefundedCount,
                    -- Final paid for current student sem
                    (SELECT COUNT(*) FROM Assessments a
                     WHERE a.StudentID  = s.StudentID
                       AND a.SchoolYear = s.SchoolYear
                       AND a.Semester   = s.Semester
                       AND a.Period     = 'Final'
                       AND a.Status     = 'Paid') AS FinalPaid,
                    -- Latest refunded semester details (may differ from current student record)
                    (SELECT TOP 1 a.SchoolYear FROM Assessments a
                     WHERE a.StudentID = s.StudentID AND a.Status = 'Refunded'
                     ORDER BY a.YearLevel DESC,
                              CASE a.Semester WHEN '2nd' THEN 2 ELSE 1 END DESC) AS RefSY,
                    (SELECT TOP 1 a.Semester FROM Assessments a
                     WHERE a.StudentID = s.StudentID AND a.Status = 'Refunded'
                     ORDER BY a.YearLevel DESC,
                              CASE a.Semester WHEN '2nd' THEN 2 ELSE 1 END DESC) AS RefSem,
                    (SELECT TOP 1 a.YearLevel FROM Assessments a
                     WHERE a.StudentID = s.StudentID AND a.Status = 'Refunded'
                     ORDER BY a.YearLevel DESC,
                              CASE a.Semester WHEN '2nd' THEN 2 ELSE 1 END DESC) AS RefYL
            FROM    Students s
            JOIN    Courses c ON c.CourseID = s.CourseID
            WHERE   s.Status IN ('Enrolled','Active')
            ORDER BY s.LastName, s.FirstName", con))
                {
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int activeCount = Convert.ToInt32(r["ActiveCount"]);
                            int refundedCount = Convert.ToInt32(r["RefundedCount"]);
                            int finalPaid = Convert.ToInt32(r["FinalPaid"]);
                            string sem = r["Semester"].ToString();
                            int year = Convert.ToInt32(r["YearLevel"]);
                            string sy = r["SchoolYear"].ToString();

                            // NeedsRegenerate: current semester on Students record has only Refunded rows
                            bool needsRegenerate = (refundedCount > 0 && activeCount == 0);
                            bool canGenerate = (activeCount == 0 && refundedCount == 0);
                            bool nextCanGenerate = (finalPaid == 1 && !needsRegenerate);
                            bool isGraduating = (year == 4 && sem == "2nd");

                            // Refunded semester details — use Assessment table values, not Students table
                            string refSY = r["RefSY"] == DBNull.Value ? sy : r["RefSY"].ToString();
                            string refSem = r["RefSem"] == DBNull.Value ? sem : r["RefSem"].ToString();
                            int refYL = r["RefYL"] == DBNull.Value ? year : Convert.ToInt32(r["RefYL"]);

                            string nextSem, nextSY;
                            int nextYear = year;
                            if (sem == "1st")
                            {
                                nextSem = "2nd"; nextSY = sy;
                            }
                            else
                            {
                                nextSem = "1st"; nextYear = year + 1;
                                var pts = sy.Split('-');
                                nextSY = pts.Length == 2
                                    ? (int.Parse(pts[0]) + 1) + "-" + (int.Parse(pts[1]) + 1)
                                    : sy;
                            }

                            data.Add(new
                            {
                                StudentID = r["StudentID"],
                                StudentName = r["StudentName"],
                                CourseCode = r["CourseCode"],
                                YearLevel = year,
                                Semester = sem,
                                SchoolYear = sy,
                                CanGenerate = canGenerate,
                                NeedsRegenerate = needsRegenerate,
                                NextCanGenerate = nextCanGenerate,
                                NextSem = nextSem,
                                NextYear = nextYear,
                                NextSY = nextSY,
                                IsGraduating = isGraduating,
                                RefundedSY = refSY,
                                RefundedSem = refSem,
                                RefundedYL = refYL
                            });
                        }
                    }
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // Get assessments for a specific student
        public ActionResult GetStudentAssessments(int studentID)
        {
            var data = new List<object>();
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
            SELECT  a.AssessmentID, a.Period, a.YearLevel,
                    a.SchoolYear, a.Semester,
                    a.AmountDue, a.Status,
                    ISNULL(SUM(CASE WHEN p.TransactionType = 'Payment' THEN p.AmountPaid ELSE 0 END), 0) AS TotalPaid
            FROM    Assessments a
            LEFT JOIN Payments p ON p.AssessmentID = a.AssessmentID
            WHERE   a.StudentID = @sid
            GROUP BY a.AssessmentID, a.Period, a.YearLevel,
                     a.SchoolYear, a.Semester,
                     a.AmountDue, a.Status
            ORDER BY a.YearLevel ASC,
                     CASE a.Semester WHEN '1st' THEN 1 ELSE 2 END ASC,
                     CASE a.Period
                         WHEN 'Prelim' THEN 1
                         WHEN 'Midterm' THEN 2
                         WHEN 'Semi-Final' THEN 3
                         WHEN 'Final' THEN 4
                     END ASC", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            decimal amountDue = Convert.ToDecimal(r["AmountDue"]);
                            decimal totalPaid = Convert.ToDecimal(r["TotalPaid"]);
                            decimal balance = amountDue - totalPaid;
                            if (balance < 0) balance = 0;

                            data.Add(new
                            {
                                AssessmentID = Convert.ToInt32(r["AssessmentID"]),  // INTEGER ID
                                Period = r["Period"].ToString(),
                                YearLevel = Convert.ToInt32(r["YearLevel"]),
                                Semester = r["Semester"].ToString(),
                                SchoolYear = r["SchoolYear"].ToString(),
                                AmountDue = amountDue,
                                TotalPaid = totalPaid,
                                Balance = balance,
                                Status = r["Status"].ToString()
                            });
                        }
                    }
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult RegenerateAfterRefund()
        {
            int studentID = 0;
            string schoolYear = "", semester = "";
            int yearLevel = 0;

            if (Request["studentID"] != null) studentID = int.Parse(Request["studentID"]);
            if (Request["schoolYear"] != null) schoolYear = Request["schoolYear"];
            if (Request["semester"] != null) semester = Request["semester"];
            if (Request["yearLevel"] != null) yearLevel = int.Parse(Request["yearLevel"]);

            if (!IsAdmin())
                return Json(new { mess = 0, message = "Unauthorized." });

            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();
                    SqlTransaction trans = con.BeginTransaction();
                    try
                    {
                        // Get courseID only — do NOT read YearLevel/Semester from Students table
                        // because the student may have already advanced beyond the refunded semester
                        int courseID = 0;
                        using (var cmd = new SqlCommand(
                            "SELECT CourseID FROM Students WHERE StudentID=@sid", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentID);
                            courseID = (int)cmd.ExecuteScalar();
                        }

                        // Verify that the rows being regenerated are all Refunded
                        // (no active rows exist for this exact semester)
                        int active = 0;
                        using (var cmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM Assessments
                    WHERE StudentID  = @sid
                      AND SchoolYear = @sy
                      AND Semester   = @sem
                      AND YearLevel  = @yl
                      AND Status    != 'Refunded'", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentID);
                            cmd.Parameters.AddWithValue("@sy", schoolYear);
                            cmd.Parameters.AddWithValue("@sem", semester);
                            cmd.Parameters.AddWithValue("@yl", yearLevel);
                            active = (int)cmd.ExecuteScalar();
                        }

                        if (active > 0)
                        {
                            trans.Rollback();
                            return Json(new
                            {
                                mess = 0,
                                message = "Active assessments already exist for this semester."
                            });
                        }

                        // Use the semester/year FROM THE REQUEST — not from Students table
                        // This is the critical fix: yearLevel, semester, schoolYear come from
                        // the refunded assessment rows, not the student's current position
                        StudentController.GenerateAssessments(
                            con, studentID, courseID,
                            yearLevel,   // from request — the refunded semester's year
                            semester,    // from request — the refunded semester
                            schoolYear,  // from request — the refunded school year
                            trans);

                        trans.Commit();
                        return Json(new
                        {
                            mess = 1,
                            message = "New assessments generated for " + semester +
                                      " Semester " + schoolYear + " Year " + yearLevel +
                                      ". Student can now pay again."
                        });
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        return Json(new { mess = 0, message = ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { mess = 0, message = "Error: " + ex.Message });
            }
        }

        // Generate assessments for current semester
        public ActionResult GenerateAssessment()
        {
            int studentID = 0;
            if (Request["studentID"] != null)
            {
                studentID = int.Parse(Request["studentID"]);
            }

            if (!IsAdmin())
                return Json(new { mess = 0, message = "Unauthorized." });

            try
            {
                int courseID = 0, yearLevel = 0;
                string semester = "", schoolYear = "";

                using (var con = new SqlConnection(_conn))
                {
                    con.Open();

                    // Get student's current info
                    using (var cmd = new SqlCommand(
                        "SELECT CourseID, YearLevel, Semester, SchoolYear FROM Students WHERE StudentID=@sid", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read())
                                return Json(new { mess = 0, message = "Student not found." });
                            courseID = (int)r["CourseID"];
                            yearLevel = (int)r["YearLevel"];
                            semester = r["Semester"].ToString();
                            schoolYear = r["SchoolYear"].ToString();
                        }
                    }

                    // Check not already generated
                    int existing = 0;
                    using (var cmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM Assessments
                          WHERE StudentID=@sid AND SchoolYear=@sy AND Semester=@sem", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        cmd.Parameters.AddWithValue("@sy", schoolYear);
                        cmd.Parameters.AddWithValue("@sem", semester);
                        existing = (int)cmd.ExecuteScalar();
                    }

                    if (existing > 0)
                        return Json(new { mess = 0, message = "Assessments already generated for this semester." });

                    // Call the GenerateAssessments method (you need to have this in StudentController)
                    StudentController.GenerateAssessments(
                        con, studentID, courseID, yearLevel, semester, schoolYear);
                }

                return Json(new
                {
                    mess = 1,
                    message = $"Assessments generated for Student {studentID} — Year {yearLevel}, {semester} Semester, {schoolYear}."
                });
            }
            catch (Exception ex)
            {
                return Json(new { mess = 0, message = "Error: " + ex.Message });
            }
        }

        // Generate next semester assessments
        public ActionResult GenerateNextSemester()
        {
            int studentID = 0;
            if (Request["studentID"] != null)
            {
                studentID = int.Parse(Request["studentID"]);
            }

            if (!IsAdmin())
                return Json(new { mess = 0, message = "Unauthorized." });

            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();

                    int yearLevel = 0, courseID = 0;
                    string semester = "", schoolYear = "";

                    using (var cmd = new SqlCommand(
                        "SELECT CourseID, YearLevel, Semester, SchoolYear FROM Students WHERE StudentID=@sid", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read())
                                return Json(new { mess = 0, message = "Student not found." });
                            courseID = (int)r["CourseID"];
                            yearLevel = (int)r["YearLevel"];
                            semester = r["Semester"].ToString();
                            schoolYear = r["SchoolYear"].ToString();
                        }
                    }

                    // Verify Final is actually Paid for current sem
                    int finalPaid = 0;
                    using (var cmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM Assessments
                          WHERE StudentID=@sid AND SchoolYear=@sy
                            AND Semester=@sem AND Period='Final' AND Status='Paid'", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        cmd.Parameters.AddWithValue("@sy", schoolYear);
                        cmd.Parameters.AddWithValue("@sem", semester);
                        finalPaid = (int)cmd.ExecuteScalar();
                    }

                    if (finalPaid == 0)
                        return Json(new
                        {
                            mess = 0,
                            message = "Final period must be fully paid before generating next semester."
                        });

                    // Graduation check - UPDATE STUDENT STATUS TO GRADUATED
                    if (yearLevel == 4 && semester == "2nd")
                    {
                        using (var cmd = new SqlCommand(
                            "UPDATE Students SET Status='Graduated' WHERE StudentID=@sid", con))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentID);
                            cmd.ExecuteNonQuery();
                        }
                        return Json(new
                        {
                            mess = 1,
                            message = $"Student {studentID} has completed Year 4, 2nd Semester and is now GRADUATED."
                        });
                    }

                    // Calculate next sem/year
                    int nextYear = yearLevel;
                    string nextSem, nextSY;
                    if (semester == "1st")
                    {
                        nextSem = "2nd";
                        nextSY = schoolYear;
                    }
                    else
                    {
                        nextSem = "1st";
                        nextYear = yearLevel + 1;
                        string[] pts = schoolYear.Split('-');
                        if (pts.Length == 2)
                        {
                            int start = int.Parse(pts[0]) + 1;
                            int end = int.Parse(pts[1]) + 1;
                            nextSY = start + "-" + end;
                        }
                        else
                        {
                            nextSY = schoolYear;
                        }
                    }

                    // Check not already generated
                    int existing = 0;
                    using (var cmd = new SqlCommand(
                        @"SELECT COUNT(*) FROM Assessments
                          WHERE StudentID=@sid AND SchoolYear=@sy AND Semester=@sem", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        cmd.Parameters.AddWithValue("@sy", nextSY);
                        cmd.Parameters.AddWithValue("@sem", nextSem);
                        existing = (int)cmd.ExecuteScalar();
                    }

                    if (existing > 0)
                        return Json(new { mess = 0, message = "Next semester assessments already generated." });

                    // Update student record to next sem/year
                    using (var cmd = new SqlCommand(
                        @"UPDATE Students SET YearLevel=@y, Semester=@s, SchoolYear=@sy
                          WHERE StudentID=@sid", con))
                    {
                        cmd.Parameters.AddWithValue("@y", nextYear);
                        cmd.Parameters.AddWithValue("@s", nextSem);
                        cmd.Parameters.AddWithValue("@sy", nextSY);
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        cmd.ExecuteNonQuery();
                    }

                    StudentController.GenerateAssessments(
                        con, studentID, courseID, nextYear, nextSem, nextSY);

                    return Json(new
                    {
                        mess = 1,
                        message = $"Next semester generated: Year {nextYear}, {nextSem} Semester, {nextSY}."
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { mess = 0, message = "Error: " + ex.Message });
            }
        }

        // Unlock a locked period
        public ActionResult Unlock()
        {
            int id = 0;
            if (Request["id"] != null)
            {
                id = int.Parse(Request["id"]);
            }

            if (!IsAdmin())
                return Json(new { mess = 0, message = "Unauthorized." });

            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();
                    using (var cmd = new SqlCommand(
                        "UPDATE Assessments SET Status='Unpaid' WHERE AssessmentID=@id AND Status='Locked'", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { mess = 1, message = "Period unlocked." });
            }
            catch (Exception ex)
            {
                return Json(new { mess = 0, message = "Error: " + ex.Message });
            }
        }

        // Get fee breakdown for modal
        public ActionResult GetStudentFeeBreakdown(int studentID)
        {
            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                // Get student + course + scholarship + discount info
                using (var cmd = new SqlCommand(@"
            SELECT  s.StudentID,
                    s.FirstName + ' ' + s.LastName AS StudentName,
                    c.CourseCode, c.CourseName,
                    c.Units, c.TuitionPerUnit,
                    c.RegistrationFee, c.MiscFee, c.LabFee,
                    sc.ScholarshipName,
                    sc.Type         AS SchType,
                    sc.DiscountPercent AS SchPct,
                    sc.DiscountAmount  AS SchAmt,
                    d.DiscountName,
                    d.Type          AS DisType,
                    d.DiscountPercent  AS DisPct,
                    d.DiscountAmount   AS DisAmt
            FROM    Students s
            JOIN    Courses c       ON c.CourseID       = s.CourseID
            LEFT JOIN Scholarships sc ON sc.ScholarshipID = s.ScholarshipID
            LEFT JOIN Discounts    d  ON d.DiscountID     = s.DiscountID
            WHERE   s.StudentID = @sid", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            return Json(new { mess = 0, message = "Student not found." },
                                        JsonRequestBehavior.AllowGet);

                        int units = (int)r["Units"];
                        decimal tuitionPerUnit = (decimal)r["TuitionPerUnit"];
                        decimal regFee = (decimal)r["RegistrationFee"];
                        decimal miscFee = (decimal)r["MiscFee"];
                        decimal labFee = (decimal)r["LabFee"];

                        decimal tuitionFee = tuitionPerUnit * units;
                        decimal totalFee = tuitionFee + regFee + miscFee + labFee;

                        // Scholarship discount
                        decimal schDiscount = 0;
                        string schName = "";
                        string schDetail = "";
                        if (r["SchType"] != DBNull.Value)
                        {
                            schName = r["ScholarshipName"].ToString();
                            if (r["SchType"].ToString() == "Percent")
                            {
                                decimal pct = (decimal)r["SchPct"];
                                schDiscount = totalFee * pct / 100;
                                schDetail = pct + "%";
                            }
                            else
                            {
                                schDiscount = (decimal)r["SchAmt"];
                                schDetail = "Fixed ₱" + schDiscount.ToString("N2");
                            }
                        }

                        // Other discount
                        decimal disDiscount = 0;
                        string disName = "";
                        string disDetail = "";
                        if (r["DisType"] != DBNull.Value)
                        {
                            disName = r["DiscountName"].ToString();
                            if (r["DisType"].ToString() == "Percent")
                            {
                                decimal pct = (decimal)r["DisPct"];
                                disDiscount = totalFee * pct / 100;
                                disDetail = pct + "%";
                            }
                            else
                            {
                                disDiscount = (decimal)r["DisAmt"];
                                disDetail = "Fixed ₱" + disDiscount.ToString("N2");
                            }
                        }

                        decimal totalDiscount = Math.Min(schDiscount + disDiscount, totalFee);
                        decimal netAssessment = Math.Max(0, totalFee - totalDiscount);

                        return Json(new
                        {
                            mess = 1,
                            studentName = r["StudentName"].ToString(),
                            courseCode = r["CourseCode"].ToString(),
                            courseName = r["CourseName"].ToString(),
                            units = units,
                            tuitionPerUnit = tuitionPerUnit,
                            tuitionFee = tuitionFee,
                            regFee = regFee,
                            miscFee = miscFee,
                            labFee = labFee,
                            totalFee = totalFee,
                            schName = schName,
                            schDetail = schDetail,
                            schDiscount = schDiscount,
                            disName = disName,
                            disDetail = disDetail,
                            disDiscount = disDiscount,
                            totalDiscount = totalDiscount,
                            netAssessment = netAssessment,
                            // Period breakdown
                            prelimAmt = Math.Round(netAssessment * 53m / 100, 2),
                            midtermAmt = Math.Round(netAssessment * 11m / 100, 2),
                            semiFinalAmt = Math.Round(netAssessment * 11m / 100, 2),
                            finalAmt = Math.Round(netAssessment * 25m / 100, 2)
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
        }
    }
}