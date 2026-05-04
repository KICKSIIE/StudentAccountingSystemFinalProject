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

        // ── INDEX ─────────────────────────────────────────────────────
        public ActionResult Index()
        {
            if (Session["Role"] == null) return RedirectToAction("Login", "Account");

            var list = new List<PaymentViewModel>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
        SELECT p.PaymentID, p.AssessmentID,
               s.StudentID AS StudentNumber,
               s.FirstName + ' ' + s.LastName AS StudentName,
               c.CourseName, a.SchoolYear, a.Semester, a.Period,
               a.AmountDue, p.AmountPaid, p.PaymentDate,
               p.PaymentMode, p.ORNumber, p.Remarks,
               a.Status
        FROM Payments p
        JOIN Assessments a ON a.AssessmentID = p.AssessmentID
        JOIN Students s    ON s.StudentID    = a.StudentID
        JOIN Courses c     ON c.CourseID     = s.CourseID
        ORDER BY p.PaymentDate DESC", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new PaymentViewModel
                        {
                            PaymentID = (int)r["PaymentID"],
                            AssessmentID = (int)r["AssessmentID"],
                            StudentNumber = r["StudentNumber"].ToString(),
                            StudentName = r["StudentName"].ToString(),
                            CourseName = r["CourseName"].ToString(),
                            SchoolYear = r["SchoolYear"].ToString(),
                            Semester = r["Semester"].ToString(),
                            Period = r["Period"].ToString(),
                            AmountDue = (decimal)r["AmountDue"],
                            AmountPaid = (decimal)r["AmountPaid"],
                            PaymentDate = (DateTime)r["PaymentDate"],
                            PaymentMethod = r["PaymentMode"].ToString(),
                            ReferenceNumber = r["ORNumber"].ToString(),
                            Remarks = r["Remarks"] == DBNull.Value ? "" : r["Remarks"].ToString(),
                            Status = r["Status"].ToString()
                        });
            }
            return View(list);
        }

        // ── CREATE GET ────────────────────────────────────────────────
        public ActionResult Create()
        {
            if (Session["Role"] == null) return RedirectToAction("Login", "Account");
            PopulateAssessments();
            return View(new PaymentFormModel { PaymentDate = DateTime.Today });
        }

        // ── DETAILS ───────────────────────────────────────────────────
        public ActionResult Details(int id)
        {
            if (Session["Role"] == null) return RedirectToAction("Login", "Account");

            PaymentViewModel model = null;
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
        SELECT p.PaymentID, p.AssessmentID,
               s.StudentID AS StudentNumber,
               s.FirstName + ' ' + s.LastName AS StudentName,
               c.CourseName, a.SchoolYear, a.Semester, a.Period,
               a.AmountDue, p.AmountPaid, p.PaymentDate,
               p.PaymentMode, p.ORNumber, p.Remarks,
               a.Status
        FROM Payments p
        JOIN Assessments a ON a.AssessmentID = p.AssessmentID
        JOIN Students s    ON s.StudentID    = a.StudentID
        JOIN Courses c     ON c.CourseID     = s.CourseID
        WHERE p.PaymentID = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                con.Open();
                using (var r = cmd.ExecuteReader())
                    if (r.Read())
                        model = new PaymentViewModel
                        {
                            PaymentID = (int)r["PaymentID"],
                            AssessmentID = (int)r["AssessmentID"],
                            StudentNumber = r["StudentNumber"].ToString(),
                            StudentName = r["StudentName"].ToString(),
                            CourseName = r["CourseName"].ToString(),
                            SchoolYear = r["SchoolYear"].ToString(),
                            Semester = r["Semester"].ToString(),
                            Period = r["Period"].ToString(),
                            AmountDue = (decimal)r["AmountDue"],
                            AmountPaid = (decimal)r["AmountPaid"],
                            PaymentDate = (DateTime)r["PaymentDate"],
                            PaymentMethod = r["PaymentMode"].ToString(),
                            ReferenceNumber = r["ORNumber"] == DBNull.Value ? "" : r["ORNumber"].ToString(),
                            Remarks = r["Remarks"] == DBNull.Value ? "" : r["Remarks"].ToString(),
                            Status = r["Status"].ToString()
                        };
            }
            if (model == null) return HttpNotFound();
            return View(model);
        }

        // ── GET ASSESSMENT BALANCE (AJAX) ─────────────────────────────
        public JsonResult GetBalance(int assessmentID)
        {
            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();
                    decimal amountDue = 0, totalPaid = 0; // total paid not used

                    using (var cmd = new SqlCommand(@"
                        SELECT a.AmountDue, a.Period, a.SchoolYear, a.Semester,
                               s.FirstName + ' ' + s.LastName AS StudentName,
                               c.CourseName
                        FROM Assessments a
                        JOIN Students s    ON s.StudentID    = a.StudentID
                        JOIN Courses c ON c.CourseID = s.CourseID
                        WHERE a.AssessmentID = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", assessmentID);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read())
                                return Json(new { error = true }, JsonRequestBehavior.AllowGet);

                            amountDue = (decimal)r["AmountDue"];
                            return Json(new
                            {
                                amountDue = amountDue,
                                period = r["Period"].ToString(),
                                schoolYear = r["SchoolYear"].ToString(),
                                semester = r["Semester"].ToString(),
                                studentName = r["StudentName"].ToString(),
                                courseName = r["CourseName"].ToString()
                            }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
            }
            catch { return Json(new { error = true }, JsonRequestBehavior.AllowGet); }
        }

        // ── HELPER ────────────────────────────────────────────────────
        private void PopulateAssessments()
        {
            var items = new List<SelectListItem>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
        SELECT a.AssessmentID,
               CAST(s.StudentID AS NVARCHAR) + ' — ' + s.FirstName + ' ' + s.LastName
               + ' [' + a.Period + ' ' + a.SchoolYear + ']' AS Label,
               a.Status
        FROM Assessments a
        JOIN Students s ON s.StudentID = a.StudentID
        WHERE a.Status != 'Paid'
        ORDER BY s.LastName", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        items.Add(new SelectListItem
                        {
                            Value = r["AssessmentID"].ToString(),
                            Text = r["Label"].ToString()
                        });
            }
            ViewBag.Assessments = items;
            ViewBag.PaymentMethods = new SelectList(new[]
                { "Cash", "GCash", "Bank Transfer", "Check", "Online Payment" });
        }

        //payment total
        public JsonResult GetTotalPaid(int assessmentID)
        {
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT ISNULL(SUM(AmountPaid), 0) FROM Payments WHERE AssessmentID = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", assessmentID);
                con.Open();
                decimal total = (decimal)cmd.ExecuteScalar();
                return Json(total, JsonRequestBehavior.AllowGet);
            }
        }
    }
}