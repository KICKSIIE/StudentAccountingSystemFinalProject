using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace StudentAccountingSystem.Controllers
{
    public class WithdrawalController : Controller
    {
        private readonly string _conn = ConfigurationManager
            .ConnectionStrings["StudentAccountingDB"].ConnectionString;

        public ActionResult Index()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            var list = new List<Models.WithdrawalRequest>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT w.RequestID, w.StudentID, w.RequestDate, w.Status,
                         w.PenaltyPercent, w.TotalPaid, w.PenaltyAmount,
                         w.RefundableAmount, w.ProcessedDate, w.Remarks,
                         s.StudentNumber,
                         s.FirstName + ' ' + s.LastName AS StudentName,
                         c.CourseName
                  FROM   WithdrawalRequests w
                  JOIN   Students s ON s.StudentID = w.StudentID
                  JOIN   Courses  c ON c.CourseID  = s.CourseID
                  ORDER  BY w.RequestDate DESC", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new Models.WithdrawalRequest
                        {
                            RequestID = (int)r["RequestID"],
                            StudentID = (int)r["StudentID"],
                            StudentNumber = r["StudentNumber"].ToString(),
                            StudentName = r["StudentName"].ToString(),
                            CourseName = r["CourseName"].ToString(),
                            RequestDate = (DateTime)r["RequestDate"],
                            Status = r["Status"].ToString(),
                            PenaltyPercent = (decimal)r["PenaltyPercent"],
                            TotalPaid = (decimal)r["TotalPaid"],
                            PenaltyAmount = (decimal)r["PenaltyAmount"],
                            RefundableAmount = (decimal)r["RefundableAmount"],
                            ProcessedDate = r["ProcessedDate"] == DBNull.Value
                                              ? (DateTime?)null : (DateTime)r["ProcessedDate"],
                            Remarks = r["Remarks"] == DBNull.Value
                                              ? "" : r["Remarks"].ToString()
                        });
            }
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            int adminID = (int)Session["UserID"];

            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                int studentID = 0;
                decimal penaltyAmount = 0;
                decimal refundableAmount = 0;
                string schoolYear = "";
                string semester = "";

                using (var cmd = new SqlCommand(
                    @"SELECT w.StudentID, w.PenaltyAmount, w.RefundableAmount,
                             s.SchoolYear, s.Semester
                      FROM   WithdrawalRequests w
                      JOIN   Students s ON s.StudentID = w.StudentID
                      WHERE  w.RequestID = @rid AND w.Status = 'Pending'", con))
                {
                    cmd.Parameters.AddWithValue("@rid", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                        {
                            TempData["Error"] = "Request not found or already processed.";
                            return RedirectToAction("Index");
                        }
                        studentID = (int)r["StudentID"];
                        penaltyAmount = (decimal)r["PenaltyAmount"];
                        refundableAmount = (decimal)r["RefundableAmount"];
                        schoolYear = r["SchoolYear"].ToString();
                        semester = r["Semester"].ToString();
                    }
                }

                // Cancel all non-paid assessments for this semester
                using (var cmd = new SqlCommand(
                    @"UPDATE Assessments
                      SET    Status = 'Cancelled'
                      WHERE  StudentID  = @sid
                        AND  SchoolYear = @sy
                        AND  Semester   = @sem
                        AND  Status IN ('Unpaid', 'Locked', 'Partial')", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    cmd.Parameters.AddWithValue("@sy", schoolYear);
                    cmd.Parameters.AddWithValue("@sem", semester);
                    cmd.ExecuteNonQuery();
                }

                // Get the most recent assessment to link refund to
                int linkedAssessmentID = 0;
                string linkedPeriod = "Prelim";

                using (var cmd = new SqlCommand(
                    @"SELECT TOP 1 AssessmentID, Period
                      FROM   Assessments
                      WHERE  StudentID  = @sid
                        AND  SchoolYear = @sy
                        AND  Semester   = @sem
                      ORDER BY AssessmentID DESC", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    cmd.Parameters.AddWithValue("@sy", schoolYear);
                    cmd.Parameters.AddWithValue("@sem", semester);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read())
                        {
                            linkedAssessmentID = (int)r["AssessmentID"];
                            linkedPeriod = r["Period"].ToString();
                        }
                }

                // Record penalty as a transaction if any
                if (penaltyAmount > 0 && linkedAssessmentID > 0)
                {
                    string penaltyOR = "PEN-" + DateTime.Now.ToString("yyyyMMdd") +
                                       "-" + studentID.ToString("D4");
                    using (var cmd = new SqlCommand(
                        @"INSERT INTO Payments
                            (StudentID, AssessmentID, ORNumber, AmountPaid,
                             PaymentMode, Period, SchoolYear, Semester,
                             Remarks, PaymentDate, ReceivedBy, TransactionType)
                          VALUES
                            (@sid, @aid, @or, @amount,
                             'System', @period, @sy, @sem,
                             'Withdrawal penalty deducted', GETDATE(),
                             @admin, 'Penalty')", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        cmd.Parameters.AddWithValue("@aid", linkedAssessmentID);
                        cmd.Parameters.AddWithValue("@or", penaltyOR);
                        cmd.Parameters.AddWithValue("@amount", penaltyAmount);
                        cmd.Parameters.AddWithValue("@period", linkedPeriod);
                        cmd.Parameters.AddWithValue("@sy", schoolYear);
                        cmd.Parameters.AddWithValue("@sem", semester);
                        cmd.Parameters.AddWithValue("@admin", adminID);
                        cmd.ExecuteNonQuery();
                    }
                }

                // ── KEY PART: Auto-insert refund payment ──────────────
                if (refundableAmount > 0 && linkedAssessmentID > 0)
                {
                    string refundOR = "WREF-" + DateTime.Now.ToString("yyyyMMdd") +
                                      "-" + studentID.ToString("D4");
                    using (var cmd = new SqlCommand(
                        @"INSERT INTO Payments
                            (StudentID, AssessmentID, ORNumber, AmountPaid,
                             PaymentMode, Period, SchoolYear, Semester,
                             Remarks, PaymentDate, ReceivedBy, TransactionType)
                          VALUES
                            (@sid, @aid, @or, @amount,
                             'Cash Refund', @period, @sy, @sem,
                             'Withdrawal refund — processed by admin', GETDATE(),
                             @admin, 'Refund')", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        cmd.Parameters.AddWithValue("@aid", linkedAssessmentID);
                        cmd.Parameters.AddWithValue("@or", refundOR);
                        cmd.Parameters.AddWithValue("@amount", refundableAmount);
                        cmd.Parameters.AddWithValue("@period", linkedPeriod);
                        cmd.Parameters.AddWithValue("@sy", schoolYear);
                        cmd.Parameters.AddWithValue("@sem", semester);
                        cmd.Parameters.AddWithValue("@admin", adminID);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Update student status to Withdrawn
                using (var cmd = new SqlCommand(
                    "UPDATE Students SET Status = 'Withdrawn' WHERE StudentID = @sid", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    cmd.ExecuteNonQuery();
                }

                // Mark request as Approved
                using (var cmd = new SqlCommand(
                    @"UPDATE WithdrawalRequests SET
                        Status        = 'Approved',
                        ProcessedBy   = @admin,
                        ProcessedDate = GETDATE()
                      WHERE RequestID = @rid", con))
                {
                    cmd.Parameters.AddWithValue("@admin", adminID);
                    cmd.Parameters.AddWithValue("@rid", id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Success"] = "Withdrawal approved. Refund has been automatically processed.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            int adminID = (int)Session["UserID"];

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"UPDATE WithdrawalRequests SET
                    Status        = 'Rejected',
                    ProcessedBy   = @admin,
                    ProcessedDate = GETDATE()
                  WHERE RequestID = @rid AND Status = 'Pending'", con))
            {
                cmd.Parameters.AddWithValue("@admin", adminID);
                cmd.Parameters.AddWithValue("@rid", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["Success"] = "Withdrawal request rejected.";
            return RedirectToAction("Index");
        }
    }
}