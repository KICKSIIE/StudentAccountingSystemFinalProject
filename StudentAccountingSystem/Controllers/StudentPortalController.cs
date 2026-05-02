using StudentAccountingSystem.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Reflection;
using System.Web.Mvc;

namespace StudentAccountingSystem.Controllers
{
    public class StudentPortalController : Controller
    {
        private readonly string _conn = ConfigurationManager
            .ConnectionStrings["StudentAccountingDB"].ConnectionString;

        // ── HELPER: get logged-in student's ID ────────────────────────
        private int? GetStudentID()
        {
            if (Session["Role"]?.ToString() != "Student") return null;
            var sid = Session["StudentID"];
            if (sid == null) return null;
            return (int)sid;
        }


        // ── DASHBOARD (student home) ───────────────────────────────────
        public ActionResult Index()
        {
            if (Session["Role"]?.ToString() != "Student")
                return RedirectToAction("Login", "Account");

            int? sid = GetStudentID();
            if (sid == null) return RedirectToAction("Login", "Account");

            var model = new StudentPortalDashboard();

            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                // Student info
                using (var cmd = new SqlCommand(@"
                    SELECT s.StudentNumber, 
                           s.FirstName + ' ' + s.LastName AS FullName,
                           ISNULL(c.CourseName, 'No Course Assigned') AS CourseName, -- Added safety
                           s.YearLevel, 
                           s.Semester, 
                           s.SchoolYear, 
                           s.Status
                    FROM Students s
                    LEFT JOIN Courses c ON c.CourseID = s.CourseID -- Use LEFT JOIN
                    WHERE s.StudentID = @sid", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read())
                        {
                            model.StudentNumber = r["StudentNumber"].ToString();
                            model.FullName = r["FullName"].ToString();
                            model.CourseName = r["CourseName"].ToString();
                            model.YearLevel = (int)r["YearLevel"];
                            model.Semester = r["Semester"].ToString();
                            model.SchoolYear = r["SchoolYear"].ToString();
                            model.Status = r["Status"].ToString();
                        }
                }

                // Assessment summary per period
                using (var cmd = new SqlCommand(@"
                    SELECT a.AssessmentID, a.Period, 
                           a.TuitionFee, a.RegistrationFee, a.MiscFee, a.LabFee,
                           a.TotalFee, a.DiscountAmount, a.NetAssessment,
                           a.PercentRequired, a.AmountDue, a.Status, a.AssessedAt,
                           ISNULL(SUM(CASE WHEN p.TransactionType = 'Payment' 
                                      THEN p.AmountPaid ELSE 0 END), 0) AS TotalPaid
                    FROM Assessments a
                    LEFT JOIN Payments p ON p.AssessmentID = a.AssessmentID
                    WHERE a.StudentID  = @sid
                      AND a.SchoolYear = @sy
                      AND a.Semester   = @sem
                    GROUP BY a.AssessmentID, a.Period,
                             a.TuitionFee, a.RegistrationFee, a.MiscFee, a.LabFee,
                             a.TotalFee, a.DiscountAmount, a.NetAssessment,
                             a.PercentRequired, a.AmountDue, a.Status, a.AssessedAt
                    ORDER BY a.AssessedAt", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    cmd.Parameters.AddWithValue("@sy", model.SchoolYear);
                    cmd.Parameters.AddWithValue("@sem", model.Semester);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            model.Assessments.Add(new StudentAssessmentRow
                            {
                                AssessmentID = (int)r["AssessmentID"],
                                Period = r["Period"].ToString(),
                                TuitionFee = (decimal)r["TuitionFee"],
                                RegistrationFee = (decimal)r["RegistrationFee"],
                                MiscFee = (decimal)r["MiscFee"],
                                LabFee = (decimal)r["LabFee"],
                                TotalFee = (decimal)r["TotalFee"],
                                DiscountAmount = (decimal)r["DiscountAmount"],
                                NetAssessment = (decimal)r["NetAssessment"],
                                PercentRequired = (decimal)r["PercentRequired"],
                                AmountDue = (decimal)r["AmountDue"],
                                TotalPaid = Math.Min((decimal)r["TotalPaid"], (decimal)r["AmountDue"]),
                                Balance = Math.Max(0, (decimal)r["AmountDue"] -
                                                    Math.Min((decimal)r["TotalPaid"], (decimal)r["AmountDue"])),
                                Status = r["Status"].ToString(),
                                AssessedAt = (DateTime)r["AssessedAt"]
                            });
                }

                // Scholarship and Discount applied
                // Load payments per assessment (with carryover label)
                foreach (var assessment in model.Assessments)
                {
                    using (var cmd = new SqlCommand(@"
                        SELECT p.AmountPaid, p.PaymentDate, p.PaymentMode,
                               p.ORNumber, p.Remarks, p.TransactionType
                        FROM   Payments p
                        WHERE  p.AssessmentID = @aid
                        ORDER BY p.PaymentDate ASC", con))
                    {
                        cmd.Parameters.AddWithValue("@aid", assessment.AssessmentID);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                assessment.Payments.Add(new StudentPaymentRow
                                {
                                    AmountPaid = (decimal)r["AmountPaid"],
                                    PaymentDate = (DateTime)r["PaymentDate"],
                                    PaymentMethod = r["PaymentMode"].ToString(),
                                    ReferenceNumber = r["ORNumber"] == DBNull.Value
                                                      ? "" : r["ORNumber"].ToString(),
                                    Period = assessment.Period,
                                    Remarks = r["Remarks"] == DBNull.Value
                                                      ? "" : r["Remarks"].ToString(),
                                    IsCarryover = r["Remarks"].ToString()
                                                      .Contains("Excess from previous period")
                                                   || r["Remarks"].ToString()
                                                      .Contains("Carryover")
                                });
                    }
                }

                // --- Payment history section ---
                using (var cmd = new SqlCommand(@"
                    SELECT p.AmountPaid, p.PaymentDate, p.PaymentMode, -- Change p.PaymentMethod to p.PaymentMode
                            p.ORNumber, a.Period                      -- Change p.ReferenceNumber to p.ORNumber
                    FROM Payments p
                    JOIN Assessments a ON a.AssessmentID = p.AssessmentID
                    WHERE a.StudentID = @sid
                    ORDER BY p.PaymentDate DESC", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            model.Payments.Add(new StudentPaymentRow
                            {
                                AmountPaid = (decimal)r["AmountPaid"],
                                PaymentDate = (DateTime)r["PaymentDate"],

                                // Map the SQL 'PaymentMode' to the Model's 'PaymentMethod'
                                PaymentMethod = r["PaymentMode"].ToString(),

                                // Map the SQL 'ORNumber' to the Model's 'ReferenceNumber'
                                ReferenceNumber = r["ORNumber"] == DBNull.Value
                                                    ? "" : r["ORNumber"].ToString(),
                                Period = r["Period"].ToString()
                            });
                }
            }
            // ADD IT RIGHT HERE — after the using block, before return
            Session["FullName"] = model.FullName;
            Session["StudentNumber"] = model.StudentNumber;
            Session["CourseName"] = model.CourseName;
            Session["YearLevel"] = model.YearLevel.ToString();
            Session["Semester"] = model.Semester;
            Session["SchoolYear"] = model.SchoolYear;
            Session["StudentStatus"] = model.Status;
            return View(model);
        }

        // ── PAY NOW (Student self-pay for demo) ───────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Pay(int assessmentID, decimal amountPaid)
        {
            if (Session["Role"]?.ToString() != "Student")
                return RedirectToAction("Login", "Account");

            int? sid = GetStudentID();
            if (sid == null) return RedirectToAction("Login", "Account");

            if (amountPaid <= 0)
            {
                TempData["Error"] = "Amount must be greater than zero.";
                return RedirectToAction("Index");
            }

            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                // Get assessment details
                decimal amountDue = 0;
                decimal totalPaid = 0;
                string period = "";
                string schoolYear = "";
                string semester = "";
                string status = "";

                using (var cmd = new SqlCommand(
                    @"SELECT AmountDue, Period, SchoolYear, Semester, Status
              FROM   Assessments
              WHERE  AssessmentID = @aid AND StudentID = @sid", con))
                {
                    cmd.Parameters.AddWithValue("@aid", assessmentID);
                    cmd.Parameters.AddWithValue("@sid", sid);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                        {
                            TempData["Error"] = "Assessment not found.";
                            return RedirectToAction("Index");
                        }
                        amountDue = (decimal)r["AmountDue"];
                        period = r["Period"].ToString();
                        schoolYear = r["SchoolYear"].ToString();
                        semester = r["Semester"].ToString();
                        status = r["Status"].ToString();
                    }
                }

                // Block payment if Locked
                if (status == "Locked")
                {
                    TempData["Error"] = "This assessment is locked. " +
                        "Pay the previous period first.";
                    return RedirectToAction("Index");
                }

                // Get total already paid
                using (var cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(AmountPaid),0) FROM Payments WHERE AssessmentID=@aid", con))
                {
                    cmd.Parameters.AddWithValue("@aid", assessmentID);
                    totalPaid = (decimal)cmd.ExecuteScalar();
                }

                decimal balance = amountDue - totalPaid;
                // Cap payment at AmountDue — excess goes to next period
                decimal actualPayment = Math.Min(amountPaid, amountDue);
                decimal excess = amountPaid - actualPayment;
                // Auto-generate OR Number
                string today = DateTime.Now.ToString("yyyyMMdd");
                string orPrefix = "OR-" + today + "-";
                int sequence = 1;

                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Payments WHERE ORNumber LIKE @prefix + '%'", con))
                {
                    cmd.Parameters.AddWithValue("@prefix", orPrefix);
                    sequence = (int)cmd.ExecuteScalar() + 1;
                }

                string orNumber = orPrefix + sequence.ToString("D4");

                // Insert payment
                using (var cmd = new SqlCommand(
                    @"INSERT INTO Payments
                (StudentID, AssessmentID, ORNumber, AmountPaid, PaymentMode,
                 Period, SchoolYear, Semester, Remarks, PaymentDate, ReceivedBy)
              VALUES
                (@sid, @aid, @or, @paid, 'Student Portal',
                 @period, @sy, @sem, 'Self-paid via portal', GETDATE(), @uid)", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    cmd.Parameters.AddWithValue("@aid", assessmentID);
                    cmd.Parameters.AddWithValue("@or", orNumber);
                    cmd.Parameters.AddWithValue("@paid", actualPayment); // ← was amountPaid
                    cmd.Parameters.AddWithValue("@period", period);
                    cmd.Parameters.AddWithValue("@sy", schoolYear);
                    cmd.Parameters.AddWithValue("@sem", semester);
                    cmd.Parameters.AddWithValue("@uid", Session["UserID"]);
                    cmd.ExecuteNonQuery();
                }

                // Update assessment status
                decimal newTotal = totalPaid + actualPayment;
                string newStatus = newTotal >= amountDue ? "Paid" : "Partial";

                using (var cmd = new SqlCommand(
                    "UPDATE Assessments SET Status=@status WHERE AssessmentID=@aid", con))
                {
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@aid", assessmentID);
                    cmd.ExecuteNonQuery();
                }

                // If there's excess — apply it to the next period as a pre-payment
                if (excess > 0 && newStatus == "Paid")
                {
                    var periodOrder = new[] { "Prelim", "Midterm", "Semi-Final", "Final" };
                    int currentIndex = Array.IndexOf(periodOrder, period);

                    if (currentIndex >= 0 && currentIndex < periodOrder.Length - 1)
                    {
                        string nextPeriod = periodOrder[currentIndex + 1];

                        // Get next assessment ID
                        int nextAssessmentID = 0;
                        using (var cmd = new SqlCommand(
                            @"SELECT AssessmentID FROM Assessments
              WHERE StudentID=@sid AND SchoolYear=@sy
              AND Semester=@sem AND Period=@next", con))
                        {
                            cmd.Parameters.AddWithValue("@sid", sid);
                            cmd.Parameters.AddWithValue("@sy", schoolYear);
                            cmd.Parameters.AddWithValue("@sem", semester);
                            cmd.Parameters.AddWithValue("@next", nextPeriod);
                            var result = cmd.ExecuteScalar();
                            if (result != null) nextAssessmentID = (int)result;
                        }

                        if (nextAssessmentID > 0)
                        {
                            // Get next period's amount due
                            decimal nextAmountDue = 0;
                            using (var cmd = new SqlCommand(
                                "SELECT AmountDue FROM Assessments WHERE AssessmentID=@aid", con))
                            {
                                cmd.Parameters.AddWithValue("@aid", nextAssessmentID);
                                nextAmountDue = (decimal)cmd.ExecuteScalar();
                            }

                            // Cap excess at next period's amount due
                            decimal nextAlreadyPaid = 0;
                            using (var cmd = new SqlCommand(
                                "SELECT ISNULL(SUM(AmountPaid),0) FROM Payments WHERE AssessmentID=@aid", con))
                            {
                                cmd.Parameters.AddWithValue("@aid", nextAssessmentID);
                                nextAlreadyPaid = (decimal)cmd.ExecuteScalar();
                            }

                            decimal nextRemainingBalance = Math.Max(0, nextAmountDue - nextAlreadyPaid);
                            decimal appliedExcess = Math.Min(excess, nextRemainingBalance);

                            // Only insert if there's actually something to apply
                            if (appliedExcess <= 0)
                            {
                                TempData["Success"] = $"Payment of ₱{actualPayment:N2} recorded successfully.";
                                return RedirectToAction("Index");
                            }

                            // Generate OR number for excess payment
                            string excessOR = "OR-" + DateTime.Now.ToString("yyyyMMdd") +
                                              "-EX-" + nextAssessmentID.ToString("D4");

                            // Insert excess as payment to next period
                            using (var cmd = new SqlCommand(
                                @"INSERT INTO Payments
                    (StudentID, AssessmentID, ORNumber, AmountPaid, PaymentMode,
                     Period, SchoolYear, Semester, Remarks, PaymentDate, ReceivedBy)
                  VALUES
                    (@sid, @aid, @or, @paid, 'Carryover',
                     @period, @sy, @sem, 'Excess from previous period', GETDATE(), @uid)", con))
                            {
                                cmd.Parameters.AddWithValue("@sid", sid);
                                cmd.Parameters.AddWithValue("@aid", nextAssessmentID);
                                cmd.Parameters.AddWithValue("@or", excessOR);
                                cmd.Parameters.AddWithValue("@paid", appliedExcess);
                                cmd.Parameters.AddWithValue("@period", nextPeriod);
                                cmd.Parameters.AddWithValue("@sy", schoolYear);
                                cmd.Parameters.AddWithValue("@sem", semester);
                                cmd.Parameters.AddWithValue("@uid", Session["UserID"]);
                                cmd.ExecuteNonQuery();
                            }

                            // Update next assessment status if fully covered
                            string nextStatus = appliedExcess >= nextAmountDue ? "Paid" : "Partial";
                            using (var cmd = new SqlCommand(
                                "UPDATE Assessments SET Status=@status WHERE AssessmentID=@aid", con))
                            {
                                cmd.Parameters.AddWithValue("@status", nextStatus);
                                cmd.Parameters.AddWithValue("@aid", nextAssessmentID);
                                cmd.ExecuteNonQuery();
                            }

                            // Also cascade — if next period is fully paid by excess,
                            // unlock the one after that too
                            if (nextStatus == "Paid")
                            {
                                var periodOrder2 = new[] { "Prelim", "Midterm", "Semi-Final", "Final" };
                                int nextIndex = Array.IndexOf(periodOrder2, nextPeriod);
                                if (nextIndex >= 0 && nextIndex < periodOrder2.Length - 1)
                                {
                                    string afterNext = periodOrder2[nextIndex + 1];
                                    using (var cmd2 = new SqlCommand(
                                        @"UPDATE Assessments SET Status='Unpaid'
              WHERE StudentID=@sid AND SchoolYear=@sy
              AND Semester=@sem AND Period=@p AND Status='Locked'", con))
                                    {
                                        cmd2.Parameters.AddWithValue("@sid", sid);
                                        cmd2.Parameters.AddWithValue("@sy", schoolYear);
                                        cmd2.Parameters.AddWithValue("@sem", semester);
                                        cmd2.Parameters.AddWithValue("@p", afterNext);
                                        cmd2.ExecuteNonQuery();
                                    }
                                }
                                else if (nextPeriod == "Final")
                                {
                                    StudentController.AdvanceSemester(con, sid.Value);
                                }
                            }

                            TempData["Success"] = $"Payment recorded. " +
                                $"Excess of ₱{appliedExcess:N2} applied to {nextPeriod}.";
                        }
                    }
                    else
                    {
                        // Excess after Final — true overpayment, store for refund
                        TempData["Success"] = $"Payment recorded. " +
                            $"Excess of ₱{excess:N2} is available for refund. Contact admin.";
                    }
                }
                else
                {
                    TempData["Success"] = $"Payment of ₱{actualPayment:N2} recorded successfully.";
                }

                // If fully paid — check what to unlock next
                // Unlock next period only if no excess was already applied to it
                if (newStatus == "Paid" && excess <= 0)
                {
                    var periodOrder = new[] { "Prelim", "Midterm", "Semi-Final", "Final" };
                    int currentIndex = Array.IndexOf(periodOrder, period);

                    if (currentIndex >= 0 && currentIndex < periodOrder.Length - 1)
                    {
                        string nextPeriod = periodOrder[currentIndex + 1];
                        using (var cmd = new SqlCommand(
                            @"UPDATE Assessments SET Status = 'Unpaid'
              WHERE  StudentID  = @sid
                AND  SchoolYear = @sy
                AND  Semester   = @sem
                AND  Period     = @next
                AND  Status     = 'Locked'", con))
                        {
                            cmd.Parameters.AddWithValue("@sid", sid);
                            cmd.Parameters.AddWithValue("@sy", schoolYear);
                            cmd.Parameters.AddWithValue("@sem", semester);
                            cmd.Parameters.AddWithValue("@next", nextPeriod);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else if (period == "Final")
                    {
                        StudentController.AdvanceSemester(con, sid.Value);
                    }
                }
            }

            return RedirectToAction("Index");
        }

        // POST: /StudentPortal/RequestWithdrawal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestWithdrawal()
        {
            if (Session["Role"]?.ToString() != "Student")
                return RedirectToAction("Login", "Account");

            int? sid = GetStudentID();
            if (sid == null) return RedirectToAction("Login", "Account");

            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                // Check student status
                string status = "", schoolYear = "", semester = "";
                using (var cmd = new SqlCommand(
                    "SELECT Status, SchoolYear, Semester FROM Students WHERE StudentID = @sid", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return RedirectToAction("Index");
                        status = r["Status"].ToString();
                        schoolYear = r["SchoolYear"].ToString();
                        semester = r["Semester"].ToString();
                    }
                }

                if (status != "Enrolled")
                {
                    TempData["Error"] = "Only enrolled students can request withdrawal.";
                    return RedirectToAction("Index");
                }

                // Check for existing pending request
                int existing = 0;
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM WithdrawalRequests WHERE StudentID=@sid AND Status='Pending'", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    existing = (int)cmd.ExecuteScalar();
                }
                if (existing > 0)
                {
                    TempData["Error"] = "You already have a pending withdrawal request.";
                    return RedirectToAction("Index");
                }

                // Determine penalty based on highest period paid
                decimal totalPaid = 0;
                decimal penaltyPercent = 0;
                string highestPaid = "";

                using (var cmd = new SqlCommand(
                    @"SELECT Period FROM Assessments
              WHERE  StudentID  = @sid
                AND  SchoolYear = @sy
                AND  Semester   = @sem
                AND  Status     = 'Paid'
              ORDER BY CASE Period
                WHEN 'Final'      THEN 4
                WHEN 'Semi-Final' THEN 3
                WHEN 'Midterm'    THEN 2
                WHEN 'Prelim'     THEN 1
              END DESC", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    cmd.Parameters.AddWithValue("@sy", schoolYear);
                    cmd.Parameters.AddWithValue("@sem", semester);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) highestPaid = r["Period"].ToString();
                }

                // Block withdrawal if Semi-Final or Final is paid
                if (highestPaid == "Semi-Final" || highestPaid == "Final")
                {
                    TempData["Error"] = "Withdrawal is not allowed after Semi-Final or Final has been paid.";
                    return RedirectToAction("Index");
                }

                // Set penalty percent
                if (highestPaid == "Prelim") penaltyPercent = 25;
                else if (highestPaid == "Midterm") penaltyPercent = 50;
                else penaltyPercent = 0;

                // Total paid this semester
                using (var cmd = new SqlCommand(
                    @"SELECT ISNULL(SUM(p.AmountPaid), 0)
              FROM   Payments p
              JOIN   Assessments a ON a.AssessmentID = p.AssessmentID
              WHERE  p.StudentID    = @sid
                AND  a.SchoolYear   = @sy
                AND  a.Semester     = @sem
                ", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    cmd.Parameters.AddWithValue("@sy", schoolYear);
                    cmd.Parameters.AddWithValue("@sem", semester);
                    totalPaid = (decimal)cmd.ExecuteScalar();
                }

                decimal penaltyAmount = Math.Round(totalPaid * penaltyPercent / 100, 2);
                decimal refundableAmount = Math.Max(0, totalPaid - penaltyAmount);

                // Insert withdrawal request
                using (var cmd = new SqlCommand(
                    @"INSERT INTO WithdrawalRequests
                (StudentID, RequestDate, Status, PenaltyPercent,
                 TotalPaid, PenaltyAmount, RefundableAmount)
              VALUES
                (@sid, GETDATE(), 'Pending', @pct,
                 @paid, @penalty, @refund)", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    cmd.Parameters.AddWithValue("@pct", penaltyPercent);
                    cmd.Parameters.AddWithValue("@paid", totalPaid);
                    cmd.Parameters.AddWithValue("@penalty", penaltyAmount);
                    cmd.Parameters.AddWithValue("@refund", refundableAmount);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Success"] = "Withdrawal request submitted. Please wait for admin approval.";
            return RedirectToAction("Index");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestReactivation(string targetSchoolYear,
                                         string targetSemester,
                                         int targetYearLevel)
        {
            if (Session["Role"]?.ToString() != "Student")
                return RedirectToAction("Login", "Account");

            int? sid = GetStudentID();
            if (sid == null) return RedirectToAction("Login", "Account");

            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                string status = "";
                using (var cmd = new SqlCommand(
                    "SELECT Status FROM Students WHERE StudentID = @sid", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    status = cmd.ExecuteScalar()?.ToString() ?? "";
                }

                if (status != "Withdrawn")
                {
                    TempData["Error"] = "Only withdrawn students can request reactivation.";
                    return RedirectToAction("Index");
                }

                int existing = 0;
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM ReactivationRequests WHERE StudentID=@sid AND Status='Pending'", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    existing = (int)cmd.ExecuteScalar();
                }

                if (existing > 0)
                {
                    TempData["Error"] = "You already have a pending reactivation request.";
                    return RedirectToAction("Index");
                }

                using (var cmd = new SqlCommand(
                    @"INSERT INTO ReactivationRequests
                (StudentID, RequestDate, Status,
                 TargetSchoolYear, TargetSemester, TargetYearLevel)
              VALUES
                (@sid, GETDATE(), 'Pending', @sy, @sem, @year)", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    cmd.Parameters.AddWithValue("@sy", targetSchoolYear);
                    cmd.Parameters.AddWithValue("@sem", targetSemester);
                    cmd.Parameters.AddWithValue("@year", targetYearLevel);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Success"] = "Reactivation request submitted. Please wait for admin approval.";
            return RedirectToAction("Index");
        }

    }
}