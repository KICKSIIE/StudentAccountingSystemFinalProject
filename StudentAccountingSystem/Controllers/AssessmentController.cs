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

        public ActionResult Index()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            var list = new List<AssessmentViewModel>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT a.AssessmentID,
                 s.StudentID, 
                 s.FirstName + ' ' + s.LastName AS StudentName,
                 c.CourseName,
                 a.YearLevel, -- 1. ADDED YEAR LEVEL
                 a.SchoolYear,
                 a.Semester,
                 a.Period,
                 a.TotalFee,
                 a.TuitionFee,
                 a.RegistrationFee,
                 a.MiscFee,
                 a.LabFee,
                 a.DiscountAmount,
                 a.NetAssessment,
                 a.PercentRequired,
                 a.AmountDue,
                 a.OverpaidAmount,
                 a.Status,
                 a.AssessedAt,
                 ISNULL(SUM(p.AmountPaid), 0) AS TotalPaid
          FROM   Assessments a
          JOIN   Students s  ON s.StudentID = a.StudentID
          JOIN   Courses  c  ON c.CourseID  = s.CourseID
          LEFT JOIN Payments p ON p.AssessmentID = a.AssessmentID
                               AND p.TransactionType = 'Payment'
          GROUP BY
                 a.AssessmentID, s.StudentID,
                 s.FirstName, s.LastName, c.CourseName,
                 a.YearLevel, -- 2. ADDED TO GROUP BY
                 a.SchoolYear, a.Semester, a.Period,
                 a.TotalFee, a.TuitionFee, a.RegistrationFee,
                 a.MiscFee, a.LabFee, a.DiscountAmount,
                 a.NetAssessment, a.PercentRequired, a.AmountDue,
                 a.OverpaidAmount, a.Status, a.AssessedAt
          ORDER BY s.StudentID, a.YearLevel, a.SchoolYear, a.Semester, 
                 CASE a.Period
                      WHEN 'Prelim'     THEN 1
                      WHEN 'Midterm'    THEN 2
                      WHEN 'Semi-Final' THEN 3
                      WHEN 'Final'      THEN 4
                 END", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new AssessmentViewModel
                        {
                            AssessmentID = (int)r["AssessmentID"],
                            StudentNumber = r["StudentID"].ToString(),
                            StudentName = r["StudentName"].ToString(),
                            CourseName = r["CourseName"].ToString(),
                            YearLevel = (int)r["YearLevel"], // 4. ASSIGN TO VIEWMODEL
                            SchoolYear = r["SchoolYear"].ToString(),
                            Semester = r["Semester"].ToString(),
                            Period = r["Period"].ToString(),
                            TotalFee = (decimal)r["TotalFee"],
                            TuitionFee = (decimal)r["TuitionFee"],
                            RegistrationFee = (decimal)r["RegistrationFee"],
                            MiscFee = (decimal)r["MiscFee"],
                            LabFee = (decimal)r["LabFee"],
                            DiscountAmount = (decimal)r["DiscountAmount"],
                            NetAssessment = (decimal)r["NetAssessment"],
                            PercentRequired = (decimal)r["PercentRequired"],
                            AmountDue = (decimal)r["AmountDue"],
                            OverpaidAmount = (decimal)r["OverpaidAmount"],
                            TotalPaid = (decimal)r["TotalPaid"],
                            Balance = (decimal)r["AmountDue"] - (decimal)r["TotalPaid"],
                            Status = r["Status"].ToString(),
                            AssessedAt = (DateTime)r["AssessedAt"]
                        });
                    }
                }
            }
            return View(list);
        }

        public ActionResult Details(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            AssessmentViewModel model = null;
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT a.*,
                         s.FirstName + ' ' + s.LastName AS StudentName,
                         s.StudentID,
                         c.CourseName
                  FROM   Assessments a
                  JOIN   Students s ON s.StudentID = a.StudentID
                  JOIN   Courses  c ON c.CourseID  = s.CourseID
                  WHERE  a.AssessmentID = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                con.Open();
                using (var r = cmd.ExecuteReader())
                    if (r.Read())
                        model = new AssessmentViewModel
                        {
                            AssessmentID = (int)r["AssessmentID"],
                            StudentNumber = r["StudentID"].ToString(),
                            StudentName = r["StudentName"].ToString(),
                            CourseName = r["CourseName"].ToString(),
                            SchoolYear = r["SchoolYear"].ToString(),
                            Semester = r["Semester"].ToString(),
                            Period = r["Period"].ToString(),
                            TotalFee = (decimal)r["TotalFee"],
                            TuitionFee = (decimal)r["TuitionFee"],
                            RegistrationFee = (decimal)r["RegistrationFee"],
                            MiscFee = (decimal)r["MiscFee"],
                            LabFee = (decimal)r["LabFee"],
                            DiscountAmount = (decimal)r["DiscountAmount"],
                            NetAssessment = (decimal)r["NetAssessment"],
                            PercentRequired = (decimal)r["PercentRequired"],
                            AmountDue = (decimal)r["AmountDue"],
                            OverpaidAmount = (decimal)r["OverpaidAmount"],
                            Status = r["Status"].ToString(),
                            AssessedAt = (DateTime)r["AssessedAt"]
                        };
            }
            if (model == null) return HttpNotFound();
            return View(model);
        }

        // Admin override — manually unlock an assessment
        // REPLACE the entire Unlock action
        public ActionResult Unlock(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            var data = new List<object>();
            try
            {
                using (var con = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(
                    @"UPDATE Assessments SET Status = 'Unpaid'
              WHERE AssessmentID = @id AND Status = 'Locked'", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                data.Add(new { mess = 1, message = "Assessment unlocked." });
            }
            catch
            {
                data.Add(new { mess = 0, message = "Could not unlock assessment." });
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Refunds()
        {
            // Check if Admin
            if (Session["Role"]?.ToString() != "Admin") return RedirectToAction("Login", "Account");

            var dt = new System.Data.DataTable();
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                string sql = @"SELECT r.RequestID, r.Amount, r.RequestDate, r.Status, 
                          s.FirstName + ' ' + s.LastName AS StudentName, s.StudentID AS StudentNumber
                   FROM RefundRequests r
                   JOIN Students s ON r.StudentID = s.StudentID
                   WHERE r.Status = 'Pending'
                   ORDER BY r.RequestDate DESC";

                using (var cmd = new SqlCommand(sql, con))
                {
                    using (var da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dt);
                    }
                }
            }
            return View(dt);
        }

        public ActionResult ProcessRefund(int requestId, string action)
        {
            var data = new List<object>();
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                SqlTransaction trans = con.BeginTransaction();
                try
                {
                    if (action == "Approve")
                    {
                        int studentId = 0;
                        decimal amount = 0;
                        using (var cmd = new SqlCommand(
                            "SELECT StudentID, Amount FROM RefundRequests WHERE RequestID=@rid", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@rid", requestId);
                            using (var dr = cmd.ExecuteReader())
                                if (dr.Read())
                                {
                                    studentId = (int)dr["StudentID"];
                                    amount = (decimal)dr["Amount"];
                                }
                        }

                        using (var cmd = new SqlCommand(
                            "UPDATE Assessments SET OverpaidAmount = 0 WHERE StudentID = @sid", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@sid", studentId);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new SqlCommand(
                            "UPDATE RefundRequests SET Status = 'Approved' WHERE RequestID = @rid", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@rid", requestId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cmd = new SqlCommand(
                            "UPDATE RefundRequests SET Status = 'Rejected' WHERE RequestID = @rid", con, trans))
                        {
                            cmd.Parameters.AddWithValue("@rid", requestId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    trans.Commit();
                    data.Add(new { mess = 1, message = "Refund " + action + "d successfully." });
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    data.Add(new { mess = 0, message = "Error: " + ex.Message });
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }
    }
}