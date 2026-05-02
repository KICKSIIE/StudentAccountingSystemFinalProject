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
           s.StudentNumber, s.FirstName + ' ' + s.LastName AS StudentName,
           c.CourseName, a.SchoolYear, a.Semester, a.Period,
           a.AmountDue, p.AmountPaid, p.PaymentDate,
           p.PaymentMode, p.ORNumber, p.Remarks, -- Updated these two
           a.Status
                FROM Payments p
                JOIN Assessments a ON a.AssessmentID = p.AssessmentID
                JOIN Students s    ON s.StudentID    = a.StudentID
                JOIN CourseFees cf ON cf.CourseFeeID = a.CourseFeeID
                JOIN Courses c     ON c.CourseID     = cf.CourseID
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
                            PaymentMethod = r["PaymentMode"].ToString(), // Map Mode to Method
                            ReferenceNumber = r["ORNumber"].ToString(),  // Map ORNumber to Reference
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PaymentFormModel model)
        {
            if (Session["Role"] == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid) { PopulateAssessments(); return View(model); }

            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                // Get current assessment info
                decimal amountDue = 0;
                decimal totalPaid = 0;

                using (var cmd = new SqlCommand(
                    "SELECT AmountDue FROM Assessments WHERE AssessmentID = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", model.AssessmentID);
                    var result = cmd.ExecuteScalar();
                    if (result == null)
                    {
                        ModelState.AddModelError("", "Assessment not found.");
                        PopulateAssessments();
                        return View(model);
                    }
                    amountDue = (decimal)result;
                }

                // Get total already paid for this assessment
                using (var cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(AmountPaid), 0) FROM Payments WHERE AssessmentID = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", model.AssessmentID);
                    totalPaid = (decimal)cmd.ExecuteScalar();
                }

                decimal balance = amountDue - totalPaid;

                if (model.AmountPaid <= 0)
                {
                    ModelState.AddModelError("AmountPaid", "Amount paid must be greater than zero.");
                    PopulateAssessments();
                    return View(model);
                }

                if (model.AmountPaid > balance)
                {
                    ModelState.AddModelError("AmountPaid",
                        $"Amount paid (₱{model.AmountPaid:N2}) exceeds remaining balance (₱{balance:N2}).");
                    PopulateAssessments();
                    return View(model);
                }

                // Insert payment
                using (var cmd = new SqlCommand(@"
                   INSERT INTO Payments
                        (AssessmentID, StudentID, AmountPaid, PaymentDate, PaymentMode, ORNumber, Period, SchoolYear, Semester, ReceivedBy, Remarks)
                    VALUES
                        (@aid, @sid, @paid, @date, @method, @ref, @period, @sy, @sem, @uid, @remarks)", con))
                {
                    cmd.Parameters.AddWithValue("@aid", model.AssessmentID);
                    cmd.Parameters.AddWithValue("@paid", model.AmountPaid);
                    cmd.Parameters.AddWithValue("@date", model.PaymentDate);
                    cmd.Parameters.AddWithValue("@method", model.PaymentMethod);
                    cmd.Parameters.AddWithValue("@ref", string.IsNullOrWhiteSpace(model.ReferenceNumber)
                                                            ? (object)DBNull.Value : model.ReferenceNumber.Trim());
                    cmd.Parameters.AddWithValue("@remarks", string.IsNullOrWhiteSpace(model.Remarks)
                                                            ? (object)DBNull.Value : model.Remarks.Trim());
                    cmd.ExecuteNonQuery();
                }

                // Update assessment status
                decimal newTotalPaid = totalPaid + model.AmountPaid;
                string newStatus = newTotalPaid >= amountDue ? "Paid" : "Partial";

                using (var cmd = new SqlCommand(
                    "UPDATE Assessments SET Status = @status WHERE AssessmentID = @id", con))
                {
                    cmd.Parameters.AddWithValue("@status", newStatus);
                    cmd.Parameters.AddWithValue("@id", model.AssessmentID);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Success"] = "Payment recorded successfully.";
            return RedirectToAction("Index");
        }

        // ── DETAILS ───────────────────────────────────────────────────
        public ActionResult Details(int id)
        {
            if (Session["Role"] == null) return RedirectToAction("Login", "Account");

            PaymentViewModel model = null;
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
                SELECT p.PaymentID, p.AssessmentID,
                       s.StudentNumber, s.FirstName + ' ' + s.LastName AS StudentName,
                       c.CourseName, a.SchoolYear, a.Semester, a.Period,
                       a.AmountDue, p.AmountPaid, p.PaymentDate,
                       p.PaymentMethod, p.ReferenceNumber, p.Remarks,
                       a.Status
                FROM Payments p
                JOIN Assessments a ON a.AssessmentID = p.AssessmentID
                JOIN Students s    ON s.StudentID    = a.StudentID
                JOIN Courses c ON c.CourseID = s.CourseID
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
                            PaymentMethod = r["PaymentMethod"].ToString(),
                            ReferenceNumber = r["ReferenceNumber"] == DBNull.Value ? "" : r["ReferenceNumber"].ToString(),
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
                       s.StudentNumber + ' — ' + s.FirstName + ' ' + s.LastName
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