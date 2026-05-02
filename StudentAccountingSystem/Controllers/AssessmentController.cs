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

        // ── INDEX — list all assessments ──────────────────────────────
        public ActionResult Index()
        {
            if (Session["Role"] == null) return RedirectToAction("Login", "Account");

            var list = new List<AssessmentViewModel>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
                SELECT a.AssessmentID, s.StudentID, s.FirstName + ' ' + s.LastName AS StudentName,
                       s.StudentNumber, c.CourseName, a.SchoolYear, a.Semester,
                       a.Period, a.NetAssessment, a.AmountDue, a.Status, a.AssessedAt
                FROM Assessments a
                JOIN Students s ON s.StudentID = a.StudentID
                JOIN CourseFees cf ON cf.CourseFeeID = a.CourseFeeID
                JOIN Courses c ON c.CourseID = cf.CourseID
                ORDER BY a.AssessedAt DESC", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new AssessmentViewModel
                        {
                            AssessmentID = (int)r["AssessmentID"],
                            StudentID = (int)r["StudentID"],
                            StudentName = r["StudentName"].ToString(),
                            StudentNumber = r["StudentNumber"].ToString(),
                            CourseName = r["CourseName"].ToString(),
                            SchoolYear = r["SchoolYear"].ToString(),
                            Semester = r["Semester"].ToString(),
                            Period = r["Period"].ToString(),
                            NetAssessment = (decimal)r["NetAssessment"],
                            AmountDue = (decimal)r["AmountDue"],
                            Status = r["Status"].ToString(),
                            AssessedAt = (DateTime)r["AssessedAt"]
                        });
            }
            return View(list);
        }

        // ── CREATE GET — pick student & period ────────────────────────
        public ActionResult Create()
        {
            if (Session["Role"] == null) return RedirectToAction("Login", "Account");
            PopulateDropdowns();
            return View(new AssessmentFormModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AssessmentFormModel model)
        {
            if (Session["Role"] == null) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid) { PopulateDropdowns(); return View(model); }

            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                // Get CourseFee row for this student's course
                int courseFeeID = 0;
                decimal tuitionPerUnit = 0, regFee = 0, miscFee = 0, labFee = 0;
                int units = 0;

                using (var cmd = new SqlCommand(@"
                    SELECT cf.CourseFeeID, cf.TuitionPerUnit, cf.RegistrationFee,
                           cf.MiscellaneousFee, cf.LaboratoryFee, cf.Units
                    FROM CourseFees cf
                    JOIN Students s ON s.CourseID = cf.CourseID
                    WHERE s.StudentID = @sid
                      AND cf.SchoolYear = @sy
                      AND cf.Semester   = @sem", con))
                {
                    cmd.Parameters.AddWithValue("@sid", model.StudentID);
                    cmd.Parameters.AddWithValue("@sy", model.SchoolYear);
                    cmd.Parameters.AddWithValue("@sem", model.Semester);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                        {
                            ModelState.AddModelError("", "No course fee found for this student, school year, and semester. Please set up Course Fees first.");
                            PopulateDropdowns();
                            return View(model);
                        }
                        courseFeeID = (int)r["CourseFeeID"];
                        tuitionPerUnit = (decimal)r["TuitionPerUnit"];
                        regFee = (decimal)r["RegistrationFee"];
                        miscFee = (decimal)r["MiscellaneousFee"];
                        labFee = (decimal)r["LaboratoryFee"];
                        units = (int)r["Units"];
                    }
                }

                // Check duplicate assessment for same student + period
                using (var cmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM Assessments
                    WHERE StudentID = @sid AND SchoolYear = @sy
                      AND Semester = @sem AND Period = @period", con))
                {
                    cmd.Parameters.AddWithValue("@sid", model.StudentID);
                    cmd.Parameters.AddWithValue("@sy", model.SchoolYear);
                    cmd.Parameters.AddWithValue("@sem", model.Semester);
                    cmd.Parameters.AddWithValue("@period", model.Period);
                    if ((int)cmd.ExecuteScalar() > 0)
                    {
                        ModelState.AddModelError("", "This student already has a " + model.Period + " assessment for this semester.");
                        PopulateDropdowns();
                        return View(model);
                    }
                }

                // Get total scholarship + discount for this student
                decimal discountAmount = 0;
                using (var cmd = new SqlCommand(@"
                    SELECT ISNULL(SUM(
                        CASE WHEN sc.Type = 'Percent'
                             THEN (cf2.TuitionPerUnit * cf2.Units + cf2.RegistrationFee + cf2.MiscellaneousFee + cf2.LaboratoryFee) * sc.DiscountPercent / 100
                             ELSE sc.DiscountAmount END), 0)
                    FROM StudentScholarships ss
                    JOIN Scholarships sc ON sc.ScholarshipID = ss.ScholarshipID
                    JOIN Students s2 ON s2.StudentID = ss.StudentID
                    JOIN CourseFees cf2 ON cf2.CourseFeeID = @cfid
                    WHERE ss.StudentID = @sid AND sc.IsActive = 1", con))
                {
                    cmd.Parameters.AddWithValue("@cfid", courseFeeID);
                    cmd.Parameters.AddWithValue("@sid", model.StudentID);
                    discountAmount += (decimal)cmd.ExecuteScalar();
                }
                using (var cmd = new SqlCommand(@"
                    SELECT ISNULL(SUM(
                        CASE WHEN d.Type = 'Percent'
                             THEN (cf2.TuitionPerUnit * cf2.Units + cf2.RegistrationFee + cf2.MiscellaneousFee + cf2.LaboratoryFee) * d.DiscountPercent / 100
                             ELSE d.DiscountAmount END), 0)
                    FROM StudentDiscounts sd
                    JOIN Discounts d ON d.DiscountID = sd.DiscountID
                    JOIN CourseFees cf2 ON cf2.CourseFeeID = @cfid
                    WHERE sd.StudentID = @sid AND d.IsActive = 1", con))
                {
                    cmd.Parameters.AddWithValue("@cfid", courseFeeID);
                    cmd.Parameters.AddWithValue("@sid", model.StudentID);
                    discountAmount += (decimal)cmd.ExecuteScalar();
                }

                // Calculate fees
                decimal tuitionFee = tuitionPerUnit * units;
                decimal totalFee = tuitionFee + regFee + miscFee + labFee;
                decimal netAssessment = totalFee - discountAmount;
                if (netAssessment < 0) netAssessment = 0;

                decimal percentRequired = model.Period == "Prelim" ? 0.53m
                                        : model.Period == "Midterm" ? 0.64m
                                        : model.Period == "Semi-Final" ? 0.75m
                                        : 1.00m; // Final

                decimal amountDue = Math.Round(netAssessment * percentRequired, 2);

                // Insert assessment
                using (var cmd = new SqlCommand(@"
                    INSERT INTO Assessments
                        (StudentID, CourseFeeID, SchoolYear, Semester, Period,
                         TotalFee, TuitionFee, RegistrationFee, MiscFee, LabFee,
                         DiscountAmount, NetAssessment, PercentRequired, AmountDue,
                         Status, AssessedAt)
                    VALUES
                        (@sid, @cfid, @sy, @sem, @period,
                         @totalFee, @tuitionFee, @regFee, @miscFee, @labFee,
                         @discount, @netAssessment, @percent, @amountDue,
                         'Unpaid', GETDATE())", con))
                {
                    cmd.Parameters.AddWithValue("@sid", model.StudentID);
                    cmd.Parameters.AddWithValue("@cfid", courseFeeID);
                    cmd.Parameters.AddWithValue("@sy", model.SchoolYear);
                    cmd.Parameters.AddWithValue("@sem", model.Semester);
                    cmd.Parameters.AddWithValue("@period", model.Period);
                    cmd.Parameters.AddWithValue("@totalFee", totalFee);
                    cmd.Parameters.AddWithValue("@tuitionFee", tuitionFee);
                    cmd.Parameters.AddWithValue("@regFee", regFee);
                    cmd.Parameters.AddWithValue("@miscFee", miscFee);
                    cmd.Parameters.AddWithValue("@labFee", labFee);
                    cmd.Parameters.AddWithValue("@discount", discountAmount);
                    cmd.Parameters.AddWithValue("@netAssessment", netAssessment);
                    cmd.Parameters.AddWithValue("@percent", percentRequired * 100);
                    cmd.Parameters.AddWithValue("@amountDue", amountDue);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Success"] = "Assessment created successfully.";
            return RedirectToAction("Index");
        }

        // ── DETAILS ───────────────────────────────────────────────────
        public ActionResult Details(int id)
        {
            if (Session["Role"] == null) return RedirectToAction("Login", "Account");

            AssessmentViewModel model = null;
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
                SELECT a.*, s.FirstName + ' ' + s.LastName AS StudentName,
                       s.StudentNumber, c.CourseName
                FROM Assessments a
                JOIN Students s ON s.StudentID = a.StudentID
                JOIN CourseFees cf ON cf.CourseFeeID = a.CourseFeeID
                JOIN Courses c ON c.CourseID = cf.CourseID
                WHERE a.AssessmentID = @id", con))
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
                            Status = r["Status"].ToString(),
                            AssessedAt = (DateTime)r["AssessedAt"]
                        };
            }
            if (model == null) return HttpNotFound();
            return View(model);
        }

        // ── HELPERS ───────────────────────────────────────────────────
        private void PopulateDropdowns()
        {
            var students = new List<SelectListItem>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT StudentID, StudentNumber, FirstName + ' ' + LastName AS FullName FROM Students WHERE Status = 'Enrolled' ORDER BY LastName", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        students.Add(new SelectListItem
                        {
                            Value = r["StudentID"].ToString(),
                            Text = r["StudentNumber"] + " — " + r["FullName"]
                        });
            }
            ViewBag.Students = students;
            ViewBag.Periods = new SelectList(new[] { "Prelim", "Midterm", "Semi-Final", "Final" });
            ViewBag.Semesters = new SelectList(new[] { "1st", "2nd", "Summer" });
        }

        public JsonResult GetFeePreview(int studentID, string schoolYear, string semester, string period)
        {
            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();
                    decimal tuitionPerUnit = 0, regFee = 0, miscFee = 0, labFee = 0;
                    int units = 0; int courseFeeID = 0;

                    using (var cmd = new SqlCommand(@"
                SELECT cf.CourseFeeID, cf.TuitionPerUnit, cf.RegistrationFee,
                       cf.MiscellaneousFee, cf.LaboratoryFee, cf.Units
                FROM CourseFees cf
                JOIN Students s ON s.CourseID = cf.CourseID
                WHERE s.StudentID = @sid AND cf.SchoolYear = @sy AND cf.Semester = @sem", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        cmd.Parameters.AddWithValue("@sy", schoolYear);
                        cmd.Parameters.AddWithValue("@sem", semester);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (!r.Read()) return Json(new { error = true }, JsonRequestBehavior.AllowGet);
                            courseFeeID = (int)r["CourseFeeID"];
                            tuitionPerUnit = (decimal)r["TuitionPerUnit"];
                            regFee = (decimal)r["RegistrationFee"];
                            miscFee = (decimal)r["MiscellaneousFee"];
                            labFee = (decimal)r["LaboratoryFee"];
                            units = (int)r["Units"];
                        }
                    }

                    decimal discount = 0;
                    using (var cmd = new SqlCommand(@"
                SELECT ISNULL(SUM(CASE WHEN sc.Type='Percent'
                    THEN (@total) * sc.DiscountPercent/100 ELSE sc.DiscountAmount END),0)
                FROM StudentScholarships ss
                JOIN Scholarships sc ON sc.ScholarshipID = ss.ScholarshipID
                WHERE ss.StudentID = @sid AND sc.IsActive = 1", con))
                    {
                        decimal total = tuitionPerUnit * units + regFee + miscFee + labFee;
                        cmd.Parameters.AddWithValue("@total", total);
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        discount += (decimal)cmd.ExecuteScalar();
                    }
                    using (var cmd = new SqlCommand(@"
                SELECT ISNULL(SUM(CASE WHEN d.Type='Percent'
                    THEN (@total) * d.DiscountPercent/100 ELSE d.DiscountAmount END),0)
                FROM StudentDiscounts sd
                JOIN Discounts d ON d.DiscountID = sd.DiscountID
                WHERE sd.StudentID = @sid AND d.IsActive = 1", con))
                    {
                        decimal total = tuitionPerUnit * units + regFee + miscFee + labFee;
                        cmd.Parameters.AddWithValue("@total", total);
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        discount += (decimal)cmd.ExecuteScalar();
                    }

                    decimal tuitionFee = tuitionPerUnit * units;
                    decimal totalFee = tuitionFee + regFee + miscFee + labFee;
                    decimal netAssessment = Math.Max(0, totalFee - discount);
                    decimal pct = period == "Prelim" ? 53 : period == "Midterm" ? 64 : period == "Semi-Final" ? 75 : 100;
                    decimal amountDue = Math.Round(netAssessment * pct / 100, 2);

                    return Json(new
                    {
                        tuitionFee,
                        registrationFee = regFee,
                        miscFee,
                        labFee,
                        totalFee,
                        discountAmount = discount,
                        netAssessment,
                        percentRequired = pct,
                        amountDue
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch { return Json(new { error = true }, JsonRequestBehavior.AllowGet); }
        }
    }
}