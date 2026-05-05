using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using StudentAccountingSystem.Models;

namespace StudentAccountingSystem.Controllers
{
    public class PaymentController : Controller
    {
        private readonly string _conn = ConfigurationManager
            .ConnectionStrings["StudentAccountingDB"].ConnectionString;

        private bool IsAdmin()
        {
            return Session["Role"] != null && Session["Role"].ToString() == "Admin";
        }

        private int GetLoggedInUserID()
        {
            return Session["UserID"] != null ? (int)Session["UserID"] : 0;
        }

        // ── PAYMENT INDEX ───────────────────────────────────────────
        public ActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        // Get all students for payment list
        public ActionResult GetStudentsForPayment()
        {
            var data = new List<object>();
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
                    SELECT  s.StudentID,
                            s.LastName + ', ' + s.FirstName + ' ' + ISNULL(s.MiddleName, '') AS StudentName,
                            c.CourseCode,
                            s.YearLevel,
                            s.Semester,
                            s.SchoolYear
                    FROM    Students s
                    JOIN    Courses c ON c.CourseID = s.CourseID
                    WHERE   s.Status IN ('Enrolled', 'Active')
                    ORDER BY s.LastName, s.FirstName", con))
                {
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            data.Add(new
                            {
                                StudentID = r["StudentID"],
                                StudentName = r["StudentName"],
                                CourseCode = r["CourseCode"],
                                YearLevel = r["YearLevel"],
                                Semester = r["Semester"],
                                SchoolYear = r["SchoolYear"]
                            });
                        }
                    }
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // Get assessments for a specific student (simplified for payment)
        public ActionResult GetStudentAssessmentsForPayment()
        {
            int studentID = 0;
            if (Request["studentID"] != null)
            {
                studentID = int.Parse(Request["studentID"]);
            }

            var data = new List<object>();
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
    SELECT  a.AssessmentID, a.Period, a.YearLevel, a.Semester,
            a.SchoolYear,
            a.AmountDue, a.Status,
            ISNULL(SUM(CASE WHEN p.TransactionType = 'Payment' THEN p.AmountPaid ELSE 0 END), 0) AS TotalPaid,
            STUFF((SELECT ', ' + CAST(p2.ORNumber AS VARCHAR)
                   FROM Payments p2
                   WHERE p2.AssessmentID = a.AssessmentID
                     AND p2.TransactionType IN ('Payment', 'Discount Adjustment')
                   ORDER BY p2.PaymentDate
                   FOR XML PATH('')), 1, 2, '') AS PaymentIDs
    FROM    Assessments a
    LEFT JOIN Payments p ON p.AssessmentID = a.AssessmentID
    WHERE   a.StudentID = @sid
    GROUP BY a.AssessmentID, a.Period, a.YearLevel, a.Semester, a.SchoolYear, a.AmountDue, a.Status
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
                                AssessmentID = Convert.ToInt32(r["AssessmentID"]),
                                Period = r["Period"].ToString(),
                                YearLevel = Convert.ToInt32(r["YearLevel"]),
                                Semester = r["Semester"].ToString(),
                                SchoolYear = r["SchoolYear"].ToString(),
                                AmountDue = amountDue,
                                TotalPaid = totalPaid,
                                Balance = balance,
                                Status = r["Status"].ToString(),
                                PaymentIDs = r["PaymentIDs"] == DBNull.Value ? "" : r["PaymentIDs"].ToString()
                            });
                        }
                    }
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // Record a payment (no [HttpPost] attribute - simple style)
        public ActionResult RecordPayment()
        {
            // Get values from Request (teacher's style)
            int assessmentID = 0;
            int studentID = 0;
            decimal amountPaid = 0;
            string remarks = "";

            if (Request["assessmentID"] != null)
                assessmentID = int.Parse(Request["assessmentID"]);
            if (Request["studentID"] != null)
                studentID = int.Parse(Request["studentID"]);
            if (Request["amountPaid"] != null)
                amountPaid = decimal.Parse(Request["amountPaid"]);
            if (Request["remarks"] != null)
                remarks = Request["remarks"];

            if (!IsAdmin())
                return Json(new { mess = 0, message = "Unauthorized." });

            if (amountPaid <= 0)
                return Json(new { mess = 0, message = "Amount must be greater than zero." });

            int receivedBy = GetLoggedInUserID();

            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();
                    SqlTransaction trans = con.BeginTransaction();

                    try
                    {
                        // Get assessment info
                        decimal amountDue = 0;
                        string period = "", schoolYear = "", semester = "", status = "";
                        int yearLevel = 0;

                        using (var cmd = new SqlCommand(
                            @"SELECT AmountDue, Period, SchoolYear, Semester, Status, YearLevel
                      FROM Assessments WHERE AssessmentID = @aid", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@aid", assessmentID);
                            using (var r = cmd.ExecuteReader())
                            {
                                if (!r.Read())
                                    throw new Exception("Assessment not found.");
                                amountDue = (decimal)r["AmountDue"];
                                period = r["Period"].ToString();
                                schoolYear = r["SchoolYear"].ToString();
                                semester = r["Semester"].ToString();
                                status = r["Status"].ToString();
                                yearLevel = (int)r["YearLevel"];
                            }
                        }

                        if (status == "Locked")
                            throw new Exception("This period is locked. Unlock it first.");
                        if (status == "Paid")
                            throw new Exception("This period is already fully paid.");
                        if (status == "Refunded")
                            throw new Exception("This period has been refunded.");

                        // Get already paid amount
                        decimal alreadyPaid = 0;
                        using (var cmd = new SqlCommand(
                            @"SELECT ISNULL(SUM(AmountPaid), 0) FROM Payments
                      WHERE AssessmentID = @aid AND TransactionType = 'Payment'", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@aid", assessmentID);
                            alreadyPaid = (decimal)cmd.ExecuteScalar();
                        }

                        decimal remaining = amountDue - alreadyPaid;
                        decimal toApply = Math.Min(amountPaid, remaining);

                        // NO EXCESS ALLOWED
                        if (amountPaid > remaining)
                        {
                            trans.Rollback();
                            return Json(new { mess = 0, message = "Amount cannot exceed balance. Balance: ₱" + remaining.ToString("N2") });
                        }

                        // Insert payment record (PaymentMode fixed to "Cash")
                        using (var cmd = new SqlCommand(@"
                    INSERT INTO Payments
                        (StudentID, AssessmentID, AmountPaid, PaymentMode,
                         Period, SchoolYear, Semester, YearLevel,
                         Remarks, PaymentDate, ReceivedBy, TransactionType)
                    VALUES
                        (@sid, @aid, @paid, 'Cash',
                         @period, @sy, @sem, @yl,
                         @remarks, GETDATE(), @recby, 'Payment')", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentID);
                            cmd.Parameters.AddWithValue("@aid", assessmentID);
                            cmd.Parameters.AddWithValue("@paid", toApply);
                            cmd.Parameters.AddWithValue("@period", period);
                            cmd.Parameters.AddWithValue("@sy", schoolYear);
                            cmd.Parameters.AddWithValue("@sem", semester);
                            cmd.Parameters.AddWithValue("@yl", yearLevel);
                            cmd.Parameters.AddWithValue("@remarks", string.IsNullOrEmpty(remarks) ? (object)DBNull.Value : remarks);
                            cmd.Parameters.AddWithValue("@recby", receivedBy == 0 ? (object)DBNull.Value : receivedBy);
                            cmd.ExecuteNonQuery();
                        }

                        decimal newTotal = alreadyPaid + toApply;
                        bool isNowPaid = newTotal >= amountDue;
                        string newStatus = isNowPaid ? "Paid" : "Partial";

                        // Update assessment status
                        using (var cmd = new SqlCommand(
                            @"UPDATE Assessments SET Status = @status
                      WHERE AssessmentID = @aid", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@status", newStatus);
                            cmd.Parameters.AddWithValue("@aid", assessmentID);
                            cmd.ExecuteNonQuery();
                        }

                        // If paid, unlock next period
                        if (isNowPaid && period != "Final")
                        {
                            string nextPeriod = period == "Prelim" ? "Midterm"
                                              : period == "Midterm" ? "Semi-Final"
                                                                    : "Final";
                            using (var cmd = new SqlCommand(
                                @"UPDATE Assessments SET Status = 'Unpaid'
                          WHERE StudentID = @sid AND SchoolYear = @sy
                            AND Semester = @sem AND Period = @next AND Status = 'Locked'", con, trans))
                            {
                                cmd.Parameters.AddWithValue("@sid", studentID);
                                cmd.Parameters.AddWithValue("@sy", schoolYear);
                                cmd.Parameters.AddWithValue("@sem", semester);
                                cmd.Parameters.AddWithValue("@next", nextPeriod);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Check if student becomes Graduated
                        bool isGraduated = false;
                        if (isNowPaid && period == "Final" && yearLevel == 4 && semester == "2nd")
                        {
                            using (var cmd = new SqlCommand(
                                @"UPDATE Students SET Status = 'Graduated' WHERE StudentID = @sid", con, trans))
                            {
                                cmd.Parameters.AddWithValue("@sid", studentID);
                                cmd.ExecuteNonQuery();
                            }
                            isGraduated = true;
                        }

                        trans.Commit();

                        // Build the appropriate message
                        string msg = "";
                        if (isNowPaid)
                        {
                            if (isGraduated)
                            {
                                msg = "✅ Final period fully paid. The student has been marked as GRADUATED. No further payments needed.";
                            }
                            else if (period == "Final")
                            {
                                msg = period + " fully paid. Go to Assessment Management to generate the next semester.";
                            }
                            else
                            {
                                msg = period + " fully paid.";
                            }
                        }
                        else
                        {
                            msg = "Partial payment recorded. Remaining balance: ₱" + (remaining - toApply).ToString("N2");
                        }

                        return Json(new
                        {
                            mess = 1,
                            message = msg,
                            finalPaid = (isNowPaid && period == "Final"),
                            isGraduated = isGraduated
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

        // ── PAYMENT HISTORY ───────────────────────────────────────────
        public ActionResult History()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");
            return View();
        }

        // Get payment history
        public ActionResult GetHistory()
        {
            int? studentID = null;
            string schoolYear = null;
            string semester = null;
            int? yearLevel = null;

            if (!string.IsNullOrEmpty(Request["studentID"]))
                studentID = int.Parse(Request["studentID"]);
            if (!string.IsNullOrEmpty(Request["schoolYear"]))
                schoolYear = Request["schoolYear"];
            if (!string.IsNullOrEmpty(Request["semester"]))
                semester = Request["semester"];
            if (!string.IsNullOrEmpty(Request["yearLevel"]))
                yearLevel = int.Parse(Request["yearLevel"]);

            var data = new List<object>();
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
            SELECT  p.PaymentID, p.ORNumber,
                    p.StudentID,
                    s.LastName + ', ' + s.FirstName + ' ' + ISNULL(s.MiddleName, '') AS StudentName,
                    p.Period, p.SchoolYear, p.Semester, p.YearLevel,
                    p.AmountPaid, p.PaymentMode, p.TransactionType,
                    p.Remarks, p.PaymentDate,
                    ISNULL(u.Username, 'System') AS ReceivedBy
            FROM    Payments p
            JOIN    Students s ON s.StudentID = p.StudentID
            LEFT JOIN Users  u ON u.UserID = p.ReceivedBy
            WHERE   (@sid IS NULL OR p.StudentID = @sid)
              AND   (@sy IS NULL OR p.SchoolYear = @sy)
              AND   (@sem IS NULL OR p.Semester = @sem)
              AND   (@yl IS NULL OR p.YearLevel = @yl)
            ORDER BY p.PaymentDate DESC", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID.HasValue ? (object)studentID.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@sy", string.IsNullOrEmpty(schoolYear) ? (object)DBNull.Value : schoolYear);
                    cmd.Parameters.AddWithValue("@sem", string.IsNullOrEmpty(semester) ? (object)DBNull.Value : semester);
                    cmd.Parameters.AddWithValue("@yl", yearLevel.HasValue ? (object)yearLevel.Value : DBNull.Value);

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            // IMPORTANT: Use lowercase property names to match JavaScript
                            data.Add(new
                            {
                                paymentID = r["PaymentID"],
                                orNumber = r["ORNumber"],
                                studentID = r["StudentID"],
                                studentName = r["StudentName"],
                                period = r["Period"],
                                schoolYear = r["SchoolYear"],
                                semester = r["Semester"],
                                yearLevel = r["YearLevel"],
                                amountPaid = r["AmountPaid"],
                                paymentMode = r["PaymentMode"],
                                transactionType = r["TransactionType"] ?? "Payment",  // Default if null
                                remarks = r["Remarks"] == DBNull.Value ? "" : r["Remarks"],
                                paymentDate = ((DateTime)r["PaymentDate"]).ToString("yyyy-MM-dd HH:mm"),
                                receivedBy = r["ReceivedBy"]
                            });
                        }
                    }
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // Get semester summary for refund
        public ActionResult GetSemesterPaymentSummary()
        {
            int studentID = 0;
            string schoolYear = "";
            string semester = "";
            int yearLevel = 0;

            if (Request["studentID"] != null)
                studentID = int.Parse(Request["studentID"]);
            if (Request["schoolYear"] != null)
                schoolYear = Request["schoolYear"];
            if (Request["semester"] != null)
                semester = Request["semester"];
            if (Request["yearLevel"] != null)
                yearLevel = int.Parse(Request["yearLevel"]);

            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                decimal totalPaid = 0;
                using (var cmd = new SqlCommand(@"
                    SELECT ISNULL(SUM(p.AmountPaid), 0)
                    FROM   Payments p
                    JOIN   Assessments a ON a.AssessmentID = p.AssessmentID
                    WHERE  a.StudentID = @sid
                      AND  a.SchoolYear = @sy
                      AND  a.Semester = @sem
                      AND  a.YearLevel = @yl
                      AND  p.TransactionType = 'Payment'", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    cmd.Parameters.AddWithValue("@sy", schoolYear);
                    cmd.Parameters.AddWithValue("@sem", semester);
                    cmd.Parameters.AddWithValue("@yl", yearLevel);
                    totalPaid = (decimal)cmd.ExecuteScalar();
                }

                int alreadyRefunded = 0;
                using (var cmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM Payments p
                    JOIN   Assessments a ON a.AssessmentID = p.AssessmentID
                    WHERE  a.StudentID = @sid
                      AND  a.SchoolYear = @sy
                      AND  a.Semester = @sem
                      AND  a.YearLevel = @yl
                      AND  p.TransactionType = 'Refund'", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    cmd.Parameters.AddWithValue("@sy", schoolYear);
                    cmd.Parameters.AddWithValue("@sem", semester);
                    cmd.Parameters.AddWithValue("@yl", yearLevel);
                    alreadyRefunded = (int)cmd.ExecuteScalar();
                }

                return Json(new
                {
                    mess = 1,
                    totalPaid = totalPaid,
                    alreadyRefunded = (alreadyRefunded > 0)
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // Process refund for entire semester
        public ActionResult ProcessRefund()
        {
            int studentID = 0;
            string schoolYear = "";
            string semester = "";
            int yearLevel = 0;
            string remarks = "";

            if (Request["studentID"] != null)
                studentID = int.Parse(Request["studentID"]);
            if (Request["schoolYear"] != null)
                schoolYear = Request["schoolYear"];
            if (Request["semester"] != null)
                semester = Request["semester"];
            if (Request["yearLevel"] != null)
                yearLevel = int.Parse(Request["yearLevel"]);
            if (Request["remarks"] != null)
                remarks = Request["remarks"];

            if (!IsAdmin())
                return Json(new { mess = 0, message = "Unauthorized." });

            int receivedBy = GetLoggedInUserID();

            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();
                    SqlTransaction trans = con.BeginTransaction();

                    try
                    {
                        // CHECK 1: Verify student is NOT Graduated
                        string studentStatus = "";
                        using (var cmd = new SqlCommand("SELECT Status FROM Students WHERE StudentID = @sid", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentID);
                            studentStatus = cmd.ExecuteScalar()?.ToString();
                        }

                        if (studentStatus == "Graduated")
                        {
                            trans.Rollback();
                            return Json(new { mess = 0, message = "Cannot refund: Student is already GRADUATED." });
                        }

                        // CHECK 2: Verify this is the LATEST semester/period the student has
                        string latestYear = "";
                        string latestSem = "";
                        int latestYearLevel = 0;
                        string latestPeriod = "";

                        using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 SchoolYear, Semester, YearLevel, Period
                    FROM Assessments 
                    WHERE StudentID = @sid 
                      AND Status != 'Refunded'
                    ORDER BY YearLevel DESC, 
                             CASE Semester WHEN '2nd' THEN 2 ELSE 1 END DESC,
                             CASE Period 
                                 WHEN 'Final' THEN 4 
                                 WHEN 'Semi-Final' THEN 3 
                                 WHEN 'Midterm' THEN 2 
                                 WHEN 'Prelim' THEN 1 
                             END DESC", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentID);
                            using (var r = cmd.ExecuteReader())
                            {
                                if (r.Read())
                                {
                                    latestYear = r["SchoolYear"].ToString();
                                    latestSem = r["Semester"].ToString();
                                    latestYearLevel = Convert.ToInt32(r["YearLevel"]);
                                    latestPeriod = r["Period"].ToString();
                                }
                            }
                        }

                        // Compare requested refund with latest
                        bool isLatest = false;

                        if (latestYearLevel > yearLevel)
                        {
                            isLatest = false;
                        }
                        else if (latestYearLevel == yearLevel)
                        {
                            if (latestSem == "2nd" && semester == "1st")
                            {
                                isLatest = false;
                            }
                            else if (latestSem == semester)
                            {
                                // Same semester, check if this is the latest period or all periods in this sem are refundable
                                isLatest = true;
                            }
                            else
                            {
                                isLatest = (latestSem == semester);
                            }
                        }
                        else if (latestYearLevel < yearLevel)
                        {
                            isLatest = false;
                        }
                        else
                        {
                            isLatest = (latestYear == schoolYear && latestSem == semester);
                        }

                        // If the semester being refunded is not the latest, check if ALL later semesters are already refunded
                        bool canRefund = false;

                        if (latestYear == schoolYear && latestSem == semester)
                        {
                            // This IS the latest semester - can refund
                            canRefund = true;
                        }
                        else
                        {
                            // Check if all semesters AFTER this one are already refunded
                            using (var cmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM Assessments a
                        WHERE a.StudentID = @sid
                          AND (
                              (a.YearLevel > @yl) OR
                              (a.YearLevel = @yl AND a.Semester = '2nd' AND @sem = '1st')
                          )
                          AND a.Status != 'Refunded'
                          AND a.Status != 'Locked'", con, trans))
                            {
                                cmd.Parameters.AddWithValue("@sid", studentID);
                                cmd.Parameters.AddWithValue("@yl", yearLevel);
                                cmd.Parameters.AddWithValue("@sem", semester);
                                int laterActiveCount = (int)cmd.ExecuteScalar();

                                canRefund = (laterActiveCount == 0);
                            }
                        }

                        if (!canRefund)
                        {
                            trans.Rollback();
                            return Json(new
                            {
                                mess = 0,
                                message = "Cannot refund this semester. You can only refund the LATEST semester/period first. Please refund newer semesters before this one."
                            });
                        }

                        // Get total paid for this semester
                        decimal totalPaid = 0;
                        using (var cmd = new SqlCommand(@"
                    SELECT ISNULL(SUM(p.AmountPaid), 0)
                    FROM   Payments p
                    JOIN   Assessments a ON a.AssessmentID = p.AssessmentID
                    WHERE  a.StudentID = @sid
                      AND  a.SchoolYear = @sy
                      AND  a.Semester = @sem
                      AND  a.YearLevel = @yl
                      AND  p.TransactionType = 'Payment'", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentID);
                            cmd.Parameters.AddWithValue("@sy", schoolYear);
                            cmd.Parameters.AddWithValue("@sem", semester);
                            cmd.Parameters.AddWithValue("@yl", yearLevel);
                            totalPaid = (decimal)cmd.ExecuteScalar();
                        }

                        if (totalPaid <= 0)
                            throw new Exception("No payments found for this semester to refund.");

                        // Check not already refunded
                        int refundExists = 0;
                        using (var cmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM Payments p
                    JOIN   Assessments a ON a.AssessmentID = p.AssessmentID
                    WHERE  a.StudentID = @sid AND a.SchoolYear = @sy
                      AND  a.Semester = @sem AND a.YearLevel = @yl
                      AND  p.TransactionType = 'Refund'", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentID);
                            cmd.Parameters.AddWithValue("@sy", schoolYear);
                            cmd.Parameters.AddWithValue("@sem", semester);
                            cmd.Parameters.AddWithValue("@yl", yearLevel);
                            refundExists = (int)cmd.ExecuteScalar();
                        }

                        if (refundExists > 0)
                            throw new Exception("A refund has already been issued for this semester.");

                        // Get the first assessment to attach refund record to
                        int firstAssessmentID = 0;
                        using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 AssessmentID FROM Assessments
                    WHERE StudentID = @sid AND SchoolYear = @sy
                      AND Semester = @sem AND YearLevel = @yl
                    ORDER BY CASE Period
                        WHEN 'Prelim' THEN 1 WHEN 'Midterm' THEN 2
                        WHEN 'Semi-Final' THEN 3 WHEN 'Final' THEN 4 END", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentID);
                            cmd.Parameters.AddWithValue("@sy", schoolYear);
                            cmd.Parameters.AddWithValue("@sem", semester);
                            cmd.Parameters.AddWithValue("@yl", yearLevel);
                            firstAssessmentID = (int)cmd.ExecuteScalar();
                        }

                        // Insert refund record
                        using (var cmd = new SqlCommand(@"
                            INSERT INTO Payments
                                (StudentID, AssessmentID, AmountPaid, PaymentMode,
                                 Period, SchoolYear, Semester, YearLevel,
                                 Remarks, PaymentDate, ReceivedBy, TransactionType)
                            VALUES
                                (@sid, @aid, @amt, 'Cash',
                                 'All Periods', @sy, @sem, @yl,
                                 @remarks, GETDATE(), @recby, 'Refund')", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentID);
                            cmd.Parameters.AddWithValue("@aid", firstAssessmentID);
                            cmd.Parameters.AddWithValue("@amt", totalPaid);
                            cmd.Parameters.AddWithValue("@sy", schoolYear);
                            cmd.Parameters.AddWithValue("@sem", semester);
                            cmd.Parameters.AddWithValue("@yl", yearLevel);
                            cmd.Parameters.AddWithValue("@remarks", string.IsNullOrEmpty(remarks) ? (object)DBNull.Value : remarks);
                            cmd.Parameters.AddWithValue("@recby", receivedBy == 0 ? (object)DBNull.Value : receivedBy);
                            cmd.ExecuteNonQuery();
                        }

                        // Mark all assessments in this semester as Refunded
                        using (var cmd = new SqlCommand(@"
                    UPDATE Assessments SET Status = 'Refunded', OverpaidAmount = 0
                    WHERE StudentID = @sid AND SchoolYear = @sy
                      AND Semester = @sem AND YearLevel = @yl", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentID);
                            cmd.Parameters.AddWithValue("@sy", schoolYear);
                            cmd.Parameters.AddWithValue("@sem", semester);
                            cmd.Parameters.AddWithValue("@yl", yearLevel);
                            cmd.ExecuteNonQuery();
                        }

                        trans.Commit();
                        // After trans.Commit(), get the PaymentID of the refund record
                        int newPaymentID = 0;
                        using (var cmd = new SqlCommand(@"
                            SELECT TOP 1 PaymentID FROM Payments 
                            WHERE StudentID = @sid AND TransactionType = 'Refund' 
                            ORDER BY PaymentDate DESC", con))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentID);
                            newPaymentID = (int)cmd.ExecuteScalar();
                        }

                        return Json(new
                        {
                            mess = 1,
                            message = "Refund of " + totalPaid.ToString("N2") + " processed successfully. Payment ID: " + newPaymentID,
                            paymentID = newPaymentID
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

        public ActionResult RegenerateAfterRefund()
        {
            int studentID = 0;
            string schoolYear = "";
            string semester = "";
            int yearLevel = 0;

            if (Request["studentID"] != null)
                studentID = int.Parse(Request["studentID"]);
            if (Request["schoolYear"] != null)
                schoolYear = Request["schoolYear"];
            if (Request["semester"] != null)
                semester = Request["semester"];
            if (Request["yearLevel"] != null)
                yearLevel = int.Parse(Request["yearLevel"]);

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
                        // Get courseID for this student
                        int courseID = 0;
                        using (var cmd = new SqlCommand("SELECT CourseID FROM Students WHERE StudentID = @sid", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentID);
                            courseID = (int)cmd.ExecuteScalar();
                        }

                        // Check if NEW assessments already exist for this semester
                        int existing = 0;
                        using (var cmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM Assessments 
                    WHERE StudentID = @sid 
                      AND SchoolYear = @sy 
                      AND Semester = @sem 
                      AND YearLevel = @yl
                      AND Status != 'Refunded'", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentID);
                            cmd.Parameters.AddWithValue("@sy", schoolYear);
                            cmd.Parameters.AddWithValue("@sem", semester);
                            cmd.Parameters.AddWithValue("@yl", yearLevel);
                            existing = (int)cmd.ExecuteScalar();
                        }

                        if (existing > 0)
                        {
                            trans.Rollback();
                            return Json(new { mess = 0, message = "Active assessments already exist for this semester. No need to regenerate." });
                        }

                        // Generate NEW assessments
                        StudentController.GenerateAssessments(con, studentID, courseID, yearLevel, semester, schoolYear, trans);

                        trans.Commit();

                        return Json(new
                        {
                            mess = 1,
                            message = "New assessments generated for " + semester + " Semester " + schoolYear + ". Student can now pay again."
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


        // Re-enable payment after refund
        public ActionResult ReenableAfterRefund()
        {
            int studentID = 0;
            string schoolYear = "";
            string semester = "";
            int yearLevel = 0;

            if (Request["studentID"] != null)
                studentID = int.Parse(Request["studentID"]);
            if (Request["schoolYear"] != null)
                schoolYear = Request["schoolYear"];
            if (Request["semester"] != null)
                semester = Request["semester"];
            if (Request["yearLevel"] != null)
                yearLevel = int.Parse(Request["yearLevel"]);

            if (!IsAdmin())
                return Json(new { mess = 0, message = "Unauthorized." });

            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();
                    using (var cmd = new SqlCommand(@"
                        UPDATE Assessments
                        SET Status = CASE Period WHEN 'Prelim' THEN 'Unpaid' ELSE 'Locked' END,
                            OverpaidAmount = 0
                        WHERE StudentID = @sid AND SchoolYear = @sy
                          AND Semester = @sem AND YearLevel = @yl
                          AND Status = 'Refunded'", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        cmd.Parameters.AddWithValue("@sy", schoolYear);
                        cmd.Parameters.AddWithValue("@sem", semester);
                        cmd.Parameters.AddWithValue("@yl", yearLevel);
                        cmd.ExecuteNonQuery();
                    }

                    return Json(new
                    {
                        mess = 1,
                        message = "Assessment re-enabled. Student can now pay again."
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { mess = 0, message = "Error: " + ex.Message });
            }
        }

        public ActionResult GenerateZeroReceipt(int assessmentID)
        {
            if (!IsAdmin())
                return Json(new { mess = 0, message = "Unauthorized." });

            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();

                    // Check if a Discount Adjustment receipt already exists
                    int existing = 0;
                    using (var cmd = new SqlCommand(@"
                SELECT COUNT(*) FROM Payments 
                WHERE AssessmentID = @aid 
                  AND TransactionType = 'Discount Adjustment'", con))
                    {
                        cmd.Parameters.AddWithValue("@aid", assessmentID);
                        existing = (int)cmd.ExecuteScalar();
                    }

                    if (existing > 0)
                    {
                        // Return the existing OR number instead of creating a new one
                        int existingOR = 0;
                        using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 ORNumber FROM Payments 
                    WHERE AssessmentID = @aid 
                      AND TransactionType = 'Discount Adjustment'
                    ORDER BY PaymentDate DESC", con))
                        {
                            cmd.Parameters.AddWithValue("@aid", assessmentID);
                            existingOR = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        return Json(new { mess = 1, orNumber = existingOR, alreadyExisted = true });
                    }

                    // Get StudentID from assessment
                    int studentID = 0;
                    using (var cmd = new SqlCommand(
                        "SELECT StudentID FROM Assessments WHERE AssessmentID = @aid", con))
                    {
                        cmd.Parameters.AddWithValue("@aid", assessmentID);
                        studentID = (int)cmd.ExecuteScalar();
                    }

                    // ← ADD HERE: Get Period, SchoolYear, Semester, YearLevel
                    string period = ""; string schoolYear = ""; string semester = ""; int yearLevel = 0;
                    using (var cmd = new SqlCommand(@"
                        SELECT Period, SchoolYear, Semester, YearLevel 
                        FROM Assessments WHERE AssessmentID = @aid", con))
                    {
                        cmd.Parameters.AddWithValue("@aid", assessmentID);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                period = r["Period"].ToString();
                                schoolYear = r["SchoolYear"].ToString();
                                semester = r["Semester"].ToString();
                                yearLevel = Convert.ToInt32(r["YearLevel"]);
                            }
                        }
                    }

                    // Insert and get new ID in one shot
                    int newPaymentID = 0;
                    using (var cmd = new SqlCommand(@"
                            INSERT INTO Payments 
                                (AssessmentID, StudentID, AmountPaid, PaymentDate, 
                                 PaymentMode, Remarks, TransactionType,
                                 Period, SchoolYear, Semester, YearLevel)
                            VALUES 
                                (@aid, @sid, 0, GETDATE(), 
                                 'Discount', 'Full discount applied - no cash collected', 'Discount Adjustment',
                                 @period, @sy, @sem, @yl);
                            SELECT SCOPE_IDENTITY();", con))
                    {
                        cmd.Parameters.AddWithValue("@aid", assessmentID);
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        cmd.Parameters.AddWithValue("@period", period);
                        cmd.Parameters.AddWithValue("@sy", schoolYear);
                        cmd.Parameters.AddWithValue("@sem", semester);
                        cmd.Parameters.AddWithValue("@yl", yearLevel);
                        newPaymentID = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Now fetch the OR number that was auto-generated
                    int orNumber = 0;
                    using (var cmd = new SqlCommand(
                        "SELECT ORNumber FROM Payments WHERE PaymentID = @pid", con))
                    {
                        cmd.Parameters.AddWithValue("@pid", newPaymentID);
                        orNumber = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    return Json(new { mess = 1, orNumber = orNumber, paymentID = newPaymentID, alreadyExisted = false });
                }
            }
            catch (Exception ex)
            {
                return Json(new { mess = 0, message = ex.Message });
            }
        }

        public ActionResult CanRefundSemester(int studentID, string schoolYear, string semester, int yearLevel)
        {
            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                // Check if this is the latest refundable semester
                bool canRefund = false;
                string message = "";

                // Check if any later semester exists that is NOT refunded
                using (var cmd = new SqlCommand(@"
            SELECT COUNT(*) FROM Assessments a
            WHERE a.StudentID = @sid
              AND a.Status != 'Refunded'
              AND (
                  (a.YearLevel > @yl) OR
                  (a.YearLevel = @yl AND a.Semester = '2nd' AND @sem = '1st')
              )", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    cmd.Parameters.AddWithValue("@yl", yearLevel);
                    cmd.Parameters.AddWithValue("@sem", semester);
                    int laterActiveCount = (int)cmd.ExecuteScalar();

                    canRefund = (laterActiveCount == 0);
                    message = canRefund ? "OK" : "Cannot refund this semester. You can only refund the LATEST semester/period first.";
                }

                return Json(new { canRefund = canRefund, message = message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}