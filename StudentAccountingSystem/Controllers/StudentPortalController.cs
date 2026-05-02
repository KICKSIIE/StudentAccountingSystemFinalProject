using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using StudentAccountingSystem.Models;

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
                           ISNULL(SUM(p.AmountPaid), 0) AS TotalPaid
                    FROM Assessments a
                    LEFT JOIN Payments p ON p.AssessmentID = a.AssessmentID
                    WHERE a.StudentID = @sid
                    GROUP BY a.AssessmentID, a.Period,
                             a.TuitionFee, a.RegistrationFee, a.MiscFee, a.LabFee,
                             a.TotalFee, a.DiscountAmount, a.NetAssessment,
                             a.PercentRequired, a.AmountDue, a.Status, a.AssessedAt
                    ORDER BY a.AssessedAt", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
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
                                TotalPaid = (decimal)r["TotalPaid"],
                                Balance = (decimal)r["AmountDue"] - (decimal)r["TotalPaid"],
                                Status = r["Status"].ToString(),
                                AssessedAt = (DateTime)r["AssessedAt"]
                            });
                }

                // Scholarships / Discounts applied
                using (var cmd = new SqlCommand(@"
                    SELECT sc.ScholarshipName, sc.Type,
                           sc.DiscountPercent, sc.DiscountAmount AS FixedAmount
                    FROM StudentScholarships ss
                    JOIN Scholarships sc ON sc.ScholarshipID = ss.ScholarshipID
                    WHERE ss.StudentID = @sid AND sc.IsActive = 1", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            model.Deductions.Add(new StudentDeductionRow
                            {
                                Name = r["ScholarshipName"].ToString(),
                                Type = r["Type"].ToString(),
                                Percent = r["DiscountPercent"] == DBNull.Value
                                              ? 0 : (decimal)r["DiscountPercent"],
                                FixedAmount = r["FixedAmount"] == DBNull.Value
                                              ? 0 : (decimal)r["FixedAmount"],
                                Category = "Scholarship"
                            });
                }

                using (var cmd = new SqlCommand(@"
                    SELECT d.DiscountName, d.Type,
                           d.DiscountPercent, d.DiscountAmount AS FixedAmount
                    FROM StudentDiscounts sd
                    JOIN Discounts d ON d.DiscountID = sd.DiscountID
                    WHERE sd.StudentID = @sid AND d.IsActive = 1", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            model.Deductions.Add(new StudentDeductionRow
                            {
                                Name = r["DiscountName"].ToString(),
                                Type = r["Type"].ToString(),
                                Percent = r["DiscountPercent"] == DBNull.Value
                                              ? 0 : (decimal)r["DiscountPercent"],
                                FixedAmount = r["FixedAmount"] == DBNull.Value
                                              ? 0 : (decimal)r["FixedAmount"],
                                Category = "Discount"
                            });
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

                if (amountPaid > balance)
                {
                    TempData["Error"] = $"Amount exceeds remaining balance of ₱{balance:N2}.";
                    return RedirectToAction("Index");
                }

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
                    cmd.Parameters.AddWithValue("@paid", amountPaid);
                    cmd.Parameters.AddWithValue("@period", period);
                    cmd.Parameters.AddWithValue("@sy", schoolYear);
                    cmd.Parameters.AddWithValue("@sem", semester);
                    cmd.Parameters.AddWithValue("@uid", Session["UserID"]);
                    cmd.ExecuteNonQuery();
                }

                // Update assessment status
                decimal newTotal = totalPaid + amountPaid;
                string newStatus = newTotal >= amountDue ? "Paid" : "Partial";

                using (var cmd = new SqlCommand(
                    "UPDATE Assessments SET Status=@status WHERE AssessmentID=@aid", con))
                {
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@aid", assessmentID);
                    cmd.ExecuteNonQuery();
                }

                // If fully paid — check what to unlock next
                if (newStatus == "Paid")
                {
                    var periodOrder = new[] { "Prelim", "Midterm", "Semi-Final", "Final" };
                    int currentIndex = Array.IndexOf(periodOrder, period);

                    if (currentIndex >= 0 && currentIndex < periodOrder.Length - 1)
                    {
                        // Unlock the next period
                        string nextPeriod = periodOrder[currentIndex + 1];
                        using (var cmd = new SqlCommand(
                            @"UPDATE Assessments SET Status = 'Unpaid'
                      WHERE  StudentID = @sid
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
                        // Final is fully paid — advance to next semester
                        StudentController.AdvanceSemester(con, sid.Value);
                    }
                }
            }

            TempData["Success"] = "Payment of ₱" + amountPaid.ToString("N2") +
                                  " recorded. OR: " + "OR-" +
                                  DateTime.Now.ToString("yyyyMMdd") + ".";
            return RedirectToAction("Index");
        }
    }
}