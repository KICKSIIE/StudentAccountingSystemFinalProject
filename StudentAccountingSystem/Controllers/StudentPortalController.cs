using StudentAccountingSystem.Controllers;
using StudentAccountingSystem.Models;
using StudentAccountingSystem.Models.StudentAccountingSystem.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
                    SELECT s.StudentID,          -- was s.StudentNumber
                           s.FirstName + ' ' + s.LastName AS FullName,
                           ISNULL(c.CourseName, 'No Course Assigned') AS CourseName,
                           s.YearLevel, 
                           s.Semester, 
                           s.SchoolYear, 
                           s.Status
                    FROM Students s
                            LEFT JOIN Courses c ON c.CourseID = s.CourseID
                    WHERE s.StudentID = @sid", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read())
                        {
                            model.StudentNumber = r["StudentID"].ToString();
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
           a.OverpaidAmount, -- <--- ADD THIS
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
             a.PercentRequired, a.AmountDue, a.Status, a.AssessedAt,
             a.OverpaidAmount -- <--- ADD THIS TO GROUP BY TOO
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
                                OverpaidAmount = (decimal)r["OverpaidAmount"],
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

                // Check overpaid balance — sum of all payments minus sum of all AmountDue
                // for fully paid semesters
                decimal prevTotalPaid = 0, prevTotalDue = 0;
                string prevSY = "", prevSem = "";

                using (var cmd = new SqlCommand(
                    @"SELECT TOP 1 SchoolYear, Semester FROM Assessments
      WHERE StudentID = @sid AND Period = 'Final' AND Status = 'Paid'
      ORDER BY AssessmentID DESC", con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read())
                        {
                            prevSY = r["SchoolYear"].ToString();
                            prevSem = r["Semester"].ToString();
                        }
                }

                if (!string.IsNullOrEmpty(prevSY))
                {
                    using (var cmd = new SqlCommand(
                        @"SELECT ISNULL(SUM(p.AmountPaid), 0)
          FROM Payments p
          JOIN Assessments a ON a.AssessmentID = p.AssessmentID
          WHERE a.StudentID=@sid AND a.SchoolYear=@sy
            AND a.Semester=@sem AND p.TransactionType='Payment'", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", sid);
                        cmd.Parameters.AddWithValue("@sy", prevSY);
                        cmd.Parameters.AddWithValue("@sem", prevSem);
                        prevTotalPaid = (decimal)cmd.ExecuteScalar();
                    }

                    using (var cmd = new SqlCommand(
                        @"SELECT ISNULL(SUM(AmountDue), 0) FROM Assessments
          WHERE StudentID=@sid AND SchoolYear=@sy AND Semester=@sem", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", sid);
                        cmd.Parameters.AddWithValue("@sy", prevSY);
                        cmd.Parameters.AddWithValue("@sem", prevSem);
                        prevTotalDue = (decimal)cmd.ExecuteScalar();
                    }
                }

                model.OverpaidBalance = Math.Max(0, prevTotalPaid - prevTotalDue);
                model.CanRequestRefund = model.OverpaidBalance > 0;

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

            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                SqlTransaction trans = con.BeginTransaction();

                try
                {
                    // 1. Get Current Assessment Info - UPDATED TO INCLUDE YearLevel
                    decimal amountDue = 0, currentAlreadyPaid = 0;
                    string period = "", schoolYear = "", semester = "", status = "";
                    int yearLevel = 1; // Default variable

                    // Added YearLevel to this SELECT
                    string getAssessmentSql = @"SELECT AmountDue, Period, SchoolYear, Semester, Status, YearLevel 
                                        FROM Assessments WHERE AssessmentID = @aid";

                    using (var cmd = new SqlCommand(getAssessmentSql, con, trans))
                    {
                        cmd.Parameters.AddWithValue("@aid", assessmentID);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) throw new Exception("Assessment not found.");
                            amountDue = (decimal)r["AmountDue"];
                            period = r["Period"].ToString();
                            schoolYear = r["SchoolYear"].ToString();
                            semester = r["Semester"].ToString();
                            status = r["Status"].ToString();
                            yearLevel = (int)r["YearLevel"]; // <--- CATCH THE YEAR LEVEL HERE
                        }
                    }

                    if (status == "Locked") throw new Exception("This period is locked.");

                    // 2. Calculate Wallet (remains same)
                    decimal totalWallet = 0;
                    using (var cmd = new SqlCommand("SELECT ISNULL(SUM(OverpaidAmount), 0) FROM Assessments WHERE StudentID = @sid", con, trans))
                    {
                        cmd.Parameters.AddWithValue("@sid", sid);
                        totalWallet = (decimal)cmd.ExecuteScalar();
                    }

                    // 3. Already Paid (remains same)
                    using (var cmd = new SqlCommand("SELECT ISNULL(SUM(AmountPaid),0) FROM Payments WHERE AssessmentID=@aid AND TransactionType='Payment'", con, trans))
                    {
                        cmd.Parameters.AddWithValue("@aid", assessmentID);
                        currentAlreadyPaid = (decimal)cmd.ExecuteScalar();
                    }

                    decimal remainingToPayForThisPeriod = amountDue - currentAlreadyPaid;
                    decimal totalAvailableFunds = totalWallet + amountPaid;

                    // 4. Step 1: Use Wallet Credit - UPDATED INSERT
                    if (totalWallet > 0)
                    {
                        decimal walletUsage = Math.Min(totalWallet, remainingToPayForThisPeriod);
                        if (walletUsage > 0)
                        {
                            string walletOR = "WLT-" + assessmentID + "-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

                            // Added YearLevel to this INSERT
                            string walletSql = @"INSERT INTO Payments (StudentID, AssessmentID, ORNumber, AmountPaid, PaymentMode, Period, SchoolYear, Semester, YearLevel, Remarks, PaymentDate, TransactionType)
                                         VALUES (@sid, @aid, @or, @paid, 'Credit Balance', @period, @sy, @sem, @yl, 'Applied from wallet', GETDATE(), 'Payment')";

                            using (var cmd = new SqlCommand(walletSql, con, trans))
                            {
                                cmd.Parameters.AddWithValue("@sid", sid);
                                cmd.Parameters.AddWithValue("@aid", assessmentID);
                                cmd.Parameters.AddWithValue("@or", walletOR);
                                cmd.Parameters.AddWithValue("@paid", walletUsage);
                                cmd.Parameters.AddWithValue("@period", period);
                                cmd.Parameters.AddWithValue("@sy", schoolYear);
                                cmd.Parameters.AddWithValue("@sem", semester);
                                cmd.Parameters.AddWithValue("@yl", yearLevel); // <--- SAVE YEAR LEVEL
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Reset wallet rows (remains same)
                        using (var cmd = new SqlCommand("UPDATE Assessments SET OverpaidAmount = 0 WHERE StudentID = @sid", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", sid);
                            cmd.ExecuteNonQuery();
                        }
                        remainingToPayForThisPeriod -= walletUsage;
                    }

                    // 5. Step 2: Process Cash Payment - UPDATED INSERT
                    if (amountPaid > 0)
                    {
                        decimal cashToApply = Math.Min(amountPaid, remainingToPayForThisPeriod);
                        string orNumber = "OR-" + DateTime.Now.ToString("yyyyMMddHHmmssfff");

                        // Added YearLevel to this INSERT
                        string cashSql = @"INSERT INTO Payments (StudentID, AssessmentID, ORNumber, AmountPaid, PaymentMode, Period, SchoolYear, Semester, YearLevel, Remarks, PaymentDate, TransactionType)
                                   VALUES (@sid, @aid, @or, @paid, 'Student Portal', @period, @sy, @sem, @yl, 'Online Payment', GETDATE(), 'Payment')";

                        using (var cmd = new SqlCommand(cashSql, con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", sid);
                            cmd.Parameters.AddWithValue("@aid", assessmentID);
                            cmd.Parameters.AddWithValue("@or", orNumber);
                            cmd.Parameters.AddWithValue("@paid", cashToApply);
                            cmd.Parameters.AddWithValue("@period", period);
                            cmd.Parameters.AddWithValue("@sy", schoolYear);
                            cmd.Parameters.AddWithValue("@sem", semester);
                            cmd.Parameters.AddWithValue("@yl", yearLevel); // <--- SAVE YEAR LEVEL
                            cmd.ExecuteNonQuery();
                        }
                        remainingToPayForThisPeriod -= cashToApply;
                    }

                    // 6. Calculate New Excess for the Wallet
                    // 6. Calculate New Excess for the Wallet
                    // Logic: Total funds minus what was actually owed for this specific row
                    decimal amountOwedBeforeThisClick = amountDue - currentAlreadyPaid;
                    decimal newExcess = Math.Max(0, totalAvailableFunds - amountOwedBeforeThisClick);
                    string newStatus = remainingToPayForThisPeriod <= 0 ? "Paid" : "Partial";

                    // 7. Update Current Assessment & Period Completion
                    using (var cmd = new SqlCommand("UPDATE Assessments SET Status=@status, OverpaidAmount=@excess WHERE AssessmentID=@aid", con, trans))
                    {
                        cmd.Parameters.AddWithValue("@status", newStatus);
                        // If the student is advancing (Final), we save 0 here and "Push" the money forward in Step 7.
                        // Otherwise, save the excess here.
                        cmd.Parameters.AddWithValue("@excess", (newStatus == "Paid" && period == "Final") ? 0 : newExcess);
                        cmd.Parameters.AddWithValue("@aid", assessmentID);
                        cmd.ExecuteNonQuery();
                    }

                    if (newStatus == "Paid")
                    {
                        if (period == "Final")
                        {
                            // Pass the 'newExcess' to ProcessAdvancement so it can "Push" it to the new semester
                            ProcessAdvancement(con, trans, sid.Value, newExcess, assessmentID);
                        }
                        else
                        {
                            UnlockNextPeriod(con, trans, sid.Value, period, schoolYear, semester);
                        }
                    }
                    trans.Commit();

                    TempData["Success"] = "Payment processed successfully!";
                }
                catch (Exception ex)
                {
                    if (trans != null) trans.Rollback();
                    TempData["Error"] = "SQL Error: " + ex.Message;
                }
            }
            return RedirectToAction("Index");
        }


        private void UnlockNextPeriod(SqlConnection con, SqlTransaction trans, int sid, string currentPeriod, string sy, string sem)
        {
            string nextPeriod = "";
            switch (currentPeriod)
            {
                case "Prelim": nextPeriod = "Midterm"; break;
                case "Midterm": nextPeriod = "Semi-Final"; break;
                case "Semi-Final": nextPeriod = "Final"; break;
                default: return; // Final is handled by ProcessAdvancement
            }

            // UPDATE the next period to 'Unpaid' so the student can see the "Pay" button
            string sql = @"UPDATE Assessments 
                   SET Status = 'Unpaid' 
                   WHERE StudentID = @sid 
                   AND SchoolYear = @sy 
                   AND Semester = @sem 
                   AND Period = @next 
                   AND Status = 'Locked'";

            using (var cmd = new SqlCommand(sql, con, trans))
            {
                cmd.Parameters.AddWithValue("@sid", sid);
                cmd.Parameters.AddWithValue("@sy", sy);
                cmd.Parameters.AddWithValue("@sem", sem);
                cmd.Parameters.AddWithValue("@next", nextPeriod);

                int rowsAffected = cmd.ExecuteNonQuery();

                // Debugging: If rowsAffected is 0, the next period row is missing from the DB
                System.Diagnostics.Debug.WriteLine("Unlocked " + rowsAffected + " rows for " + nextPeriod);
            }
        }

        // Added 'int assessmentID' to the parameters here
        private void ProcessAdvancement(SqlConnection con, SqlTransaction trans, int sid, decimal excess, int assessmentID)
        {
            int courseID = 0, yearLevel = 0;
            string sem = "", sy = "";

            using (var cmd = new SqlCommand("SELECT CourseID, YearLevel, Semester, SchoolYear FROM Students WHERE StudentID=@sid", con, trans))
            {
                cmd.Parameters.AddWithValue("@sid", sid);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        courseID = (int)r["CourseID"];
                        yearLevel = (int)r["YearLevel"];
                        sem = r["Semester"].ToString();
                        sy = r["SchoolYear"].ToString();
                    }
                }
            }

            // --- GRADUATION CHECK ---
            if (yearLevel == 4 && sem == "2nd")
            {
                // 1. Update Student Status to Graduated
                using (var cmd = new SqlCommand("UPDATE Students SET Status='Graduated' WHERE StudentID=@sid", con, trans))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    cmd.ExecuteNonQuery();
                }

                // 2. Save the remaining 14k/excess back to the Final row so it stays refundable
                using (var cmd = new SqlCommand("UPDATE Assessments SET OverpaidAmount=@excess WHERE AssessmentID=@aid", con, trans))
                {
                    cmd.Parameters.AddWithValue("@excess", excess);
                    cmd.Parameters.AddWithValue("@aid", assessmentID); // Now it knows what this is!
                    cmd.ExecuteNonQuery();
                }
                return;
            }

            // --- NORMAL ADVANCEMENT LOGIC ---
            int nextYear = yearLevel;
            string nextSem = (sem == "1st") ? "2nd" : "1st";
            string nextSY = sy;
            if (sem == "2nd")
            {
                nextYear++;
                var pts = sy.Split('-');
                if (pts.Length == 2) nextSY = (int.Parse(pts[0]) + 1) + "-" + (int.Parse(pts[1]) + 1);
            }

            using (var cmd = new SqlCommand("UPDATE Students SET YearLevel=@y, Semester=@s, SchoolYear=@sy WHERE StudentID=@sid", con, trans))
            {
                cmd.Parameters.AddWithValue("@y", nextYear);
                cmd.Parameters.AddWithValue("@s", nextSem);
                cmd.Parameters.AddWithValue("@sy", nextSY);
                cmd.Parameters.AddWithValue("@sid", sid);
                cmd.ExecuteNonQuery();
            }

            StudentController.GenerateAssessments(con, sid, courseID, nextYear, nextSem, nextSY, trans);

            // Push excess to the new Prelim
            string pushExcessSql = @"UPDATE Assessments SET OverpaidAmount = @excess 
                             WHERE StudentID = @sid AND SchoolYear = @sy 
                             AND Semester = @sem AND Period = @period";

            using (var cmd = new SqlCommand(pushExcessSql, con, trans))
            {
                cmd.Parameters.AddWithValue("@excess", excess);
                cmd.Parameters.AddWithValue("@sid", sid);
                cmd.Parameters.AddWithValue("@sy", nextSY);
                cmd.Parameters.AddWithValue("@sem", nextSem);
                cmd.Parameters.AddWithValue("@period", "Prelim");
                cmd.ExecuteNonQuery();
            }
        }

        
        // POST: Request Overpayment Refund
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestRefund(decimal amount)
        {
            int? sid = GetStudentID();
            if (sid == null) return RedirectToAction("Login", "Account");

            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                // Check if there's already a pending request to prevent double-submitting
                string checkSql = "SELECT COUNT(*) FROM RefundRequests WHERE StudentID=@sid AND Status='Pending'";
                using (var cmd = new SqlCommand(checkSql, con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    if ((int)cmd.ExecuteScalar() > 0)
                    {
                        TempData["Error"] = "You already have a pending refund request.";
                        return RedirectToAction("Index");
                    }
                }

                // Insert the request
                string sql = "INSERT INTO RefundRequests (StudentID, Amount, Status) VALUES (@sid, @amt, 'Pending')";
                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@sid", sid);
                    cmd.Parameters.AddWithValue("@amt", amount);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Success"] = "Refund request submitted successfully. Please wait for Admin approval.";
            return RedirectToAction("Index");
        }

    }
}