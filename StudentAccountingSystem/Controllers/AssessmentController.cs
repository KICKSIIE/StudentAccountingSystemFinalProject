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
                            (SELECT COUNT(*) FROM Assessments a
                             WHERE a.StudentID = s.StudentID
                               AND a.SchoolYear = s.SchoolYear
                               AND a.Semester = s.Semester) AS AssessmentCount,
                            (SELECT COUNT(*) FROM Assessments a
                             WHERE a.StudentID = s.StudentID
                               AND a.SchoolYear = s.SchoolYear
                               AND a.Semester = s.Semester
                               AND a.Period = 'Final'
                               AND a.Status = 'Paid') AS FinalPaid
                    FROM    Students s
                    JOIN    Courses c ON c.CourseID = s.CourseID
                    WHERE   s.Status IN ('Enrolled', 'Active')
                    ORDER BY s.LastName, s.FirstName", con))
                {
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int assessmentCount = Convert.ToInt32(r["AssessmentCount"]);
                            int finalPaid = Convert.ToInt32(r["FinalPaid"]);
                            string sem = r["Semester"].ToString();
                            int year = Convert.ToInt32(r["YearLevel"]);
                            string sy = r["SchoolYear"].ToString();

                            string nextSem = "";
                            string nextSY = "";
                            int nextYear = year;
                            bool isGraduating = false;

                            if (sem == "1st")
                            {
                                nextSem = "2nd";
                                nextYear = year;
                                nextSY = sy;
                            }
                            else
                            {
                                nextSem = "1st";
                                nextYear = year + 1;
                                string[] pts = sy.Split('-');
                                if (pts.Length == 2)
                                {
                                    int start = int.Parse(pts[0]) + 1;
                                    int end = int.Parse(pts[1]) + 1;
                                    nextSY = start + "-" + end;
                                }
                                else
                                {
                                    nextSY = sy;
                                }
                            }

                            if (year == 4 && sem == "2nd")
                            {
                                isGraduating = true;
                            }

                            data.Add(new
                            {
                                StudentID = r["StudentID"],
                                StudentName = r["StudentName"],
                                CourseCode = r["CourseCode"],
                                YearLevel = year,
                                Semester = sem,
                                SchoolYear = sy,
                                CanGenerate = (assessmentCount == 0),
                                NextCanGenerate = (finalPaid == 1),
                                NextSem = nextSem,
                                NextYear = nextYear,
                                NextSY = nextSY,
                                IsGraduating = isGraduating
                            });
                        }
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine("Number of students found: " + data.Count); // DEBUG
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
    }
}