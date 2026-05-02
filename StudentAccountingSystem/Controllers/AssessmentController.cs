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
                         s.StudentNumber,
                         s.FirstName + ' ' + s.LastName AS StudentName,
                         c.CourseName,
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
                         a.AssessmentID, s.StudentID, s.StudentNumber,
                         s.FirstName, s.LastName, c.CourseName,
                         a.SchoolYear, a.Semester, a.Period,
                         a.TotalFee, a.TuitionFee, a.RegistrationFee,
                         a.MiscFee, a.LabFee, a.DiscountAmount,
                         a.NetAssessment, a.PercentRequired, a.AmountDue,
                         a.OverpaidAmount, a.Status, a.AssessedAt
                  ORDER BY s.StudentNumber, a.SchoolYear, a.Semester,
                         CASE a.Period
                              WHEN 'Prelim'     THEN 1
                              WHEN 'Midterm'    THEN 2
                              WHEN 'Semi-Final' THEN 3
                              WHEN 'Final'      THEN 4
                         END", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new AssessmentViewModel
                        {
                            AssessmentID = (int)r["AssessmentID"],
                            StudentID = (int)r["StudentID"],
                            StudentNumber = r["StudentNumber"].ToString(),
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
                            TotalPaid = (decimal)r["TotalPaid"],
                            Balance = (decimal)r["AmountDue"] - (decimal)r["TotalPaid"],
                            Status = r["Status"].ToString(),
                            AssessedAt = (DateTime)r["AssessedAt"]
                        });
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
                         s.StudentNumber,
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
                            StudentID = (int)r["StudentID"],
                            StudentName = r["StudentName"].ToString(),
                            StudentNumber = r["StudentNumber"].ToString(),
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Unlock(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"UPDATE Assessments SET Status = 'Unpaid'
                  WHERE  AssessmentID = @id AND Status = 'Locked'", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            TempData["Success"] = "Assessment unlocked.";
            return RedirectToAction("Index");
        }
    }
}