using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using StudentAccountingSystem.Models;

namespace StudentAccountingSystem.Controllers
{
	public class CourseFeeController : Controller
	{
		private readonly string _conn = ConfigurationManager
			.ConnectionStrings["StudentAccountingDB"].ConnectionString;

		// ── INDEX ─────────────────────────────────────────────────────
		public ActionResult Index()
		{
			if (Session["Role"] == null) return RedirectToAction("Login", "Account");

			var list = new List<CourseFeeViewModel>();
			using (var con = new SqlConnection(_conn))
			using (var cmd = new SqlCommand(@"
                SELECT cf.CourseFeeID, c.CourseName, cf.SchoolYear, cf.Semester,
                       cf.TuitionPerUnit, cf.RegistrationFee, cf.MiscellaneousFee,
                       cf.LaboratoryFee, cf.Units,
                       (cf.TuitionPerUnit * cf.Units + cf.RegistrationFee +
                        cf.MiscellaneousFee + cf.LaboratoryFee) AS TotalFee
                FROM CourseFees cf
                JOIN Courses c ON c.CourseID = cf.CourseID
                ORDER BY cf.SchoolYear DESC, cf.Semester, c.CourseName", con))
			{
				con.Open();
				using (var r = cmd.ExecuteReader())
					while (r.Read())
						list.Add(new CourseFeeViewModel
						{
							CourseFeeID = (int)r["CourseFeeID"],
							CourseName = r["CourseName"].ToString(),
							SchoolYear = r["SchoolYear"].ToString(),
							Semester = r["Semester"].ToString(),
							TuitionPerUnit = (decimal)r["TuitionPerUnit"],
							RegistrationFee = (decimal)r["RegistrationFee"],
							MiscellaneousFee = (decimal)r["MiscellaneousFee"],
							LaboratoryFee = (decimal)r["LaboratoryFee"],
							Units = (int)r["Units"],
							TotalFee = (decimal)r["TotalFee"]
						});
			}
			return View(list);
		}

		// ── CREATE GET ────────────────────────────────────────────────
		public ActionResult Create()
		{
			if (Session["Role"] == null) return RedirectToAction("Login", "Account");
			PopulateCourses();
			return View(new CourseFeeFormModel());
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Create(CourseFeeFormModel model)
		{
			if (Session["Role"] == null) return RedirectToAction("Login", "Account");

			if (!ModelState.IsValid) { PopulateCourses(); return View(model); }

			using (var con = new SqlConnection(_conn))
			{
				con.Open();

				// Check duplicate
				using (var cmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM CourseFees
                    WHERE CourseID = @cid AND SchoolYear = @sy AND Semester = @sem", con))
				{
					cmd.Parameters.AddWithValue("@cid", model.CourseID);
					cmd.Parameters.AddWithValue("@sy", model.SchoolYear);
					cmd.Parameters.AddWithValue("@sem", model.Semester);
					if ((int)cmd.ExecuteScalar() > 0)
					{
						ModelState.AddModelError("",
							"A fee schedule already exists for this course, school year, and semester.");
						PopulateCourses();
						return View(model);
					}
				}

				using (var cmd = new SqlCommand(@"
                    INSERT INTO CourseFees
                        (CourseID, SchoolYear, Semester, TuitionPerUnit,
                         RegistrationFee, MiscellaneousFee, LaboratoryFee, Units)
                    VALUES
                        (@cid, @sy, @sem, @tpu, @reg, @misc, @lab, @units)", con))
				{
					cmd.Parameters.AddWithValue("@cid", model.CourseID);
					cmd.Parameters.AddWithValue("@sy", model.SchoolYear);
					cmd.Parameters.AddWithValue("@sem", model.Semester);
					cmd.Parameters.AddWithValue("@tpu", model.TuitionPerUnit);
					cmd.Parameters.AddWithValue("@reg", model.RegistrationFee);
					cmd.Parameters.AddWithValue("@misc", model.MiscellaneousFee);
					cmd.Parameters.AddWithValue("@lab", model.LaboratoryFee);
					cmd.Parameters.AddWithValue("@units", model.Units);
					cmd.ExecuteNonQuery();
				}
			}

			TempData["Success"] = "Course fee schedule added successfully.";
			return RedirectToAction("Index");
		}

		// ── EDIT GET ──────────────────────────────────────────────────
		public ActionResult Edit(int id)
		{
			if (Session["Role"] == null) return RedirectToAction("Login", "Account");

			CourseFeeFormModel model = null;
			using (var con = new SqlConnection(_conn))
			using (var cmd = new SqlCommand(
				"SELECT * FROM CourseFees WHERE CourseFeeID = @id", con))
			{
				cmd.Parameters.AddWithValue("@id", id);
				con.Open();
				using (var r = cmd.ExecuteReader())
					if (r.Read())
						model = new CourseFeeFormModel
						{
							CourseFeeID = (int)r["CourseFeeID"],
							CourseID = (int)r["CourseID"],
							SchoolYear = r["SchoolYear"].ToString(),
							Semester = r["Semester"].ToString(),
							TuitionPerUnit = (decimal)r["TuitionPerUnit"],
							RegistrationFee = (decimal)r["RegistrationFee"],
							MiscellaneousFee = (decimal)r["MiscellaneousFee"],
							LaboratoryFee = (decimal)r["LaboratoryFee"],
							Units = (int)r["Units"]
						};
			}
			if (model == null) return HttpNotFound();
			PopulateCourses();
			return View(model);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Edit(CourseFeeFormModel model)
		{
			if (Session["Role"] == null) return RedirectToAction("Login", "Account");

			if (!ModelState.IsValid) { PopulateCourses(); return View(model); }

			using (var con = new SqlConnection(_conn))
			using (var cmd = new SqlCommand(@"
                UPDATE CourseFees SET
                    TuitionPerUnit   = @tpu,
                    RegistrationFee  = @reg,
                    MiscellaneousFee = @misc,
                    LaboratoryFee    = @lab,
                    Units            = @units
                WHERE CourseFeeID = @id", con))
			{
				cmd.Parameters.AddWithValue("@tpu", model.TuitionPerUnit);
				cmd.Parameters.AddWithValue("@reg", model.RegistrationFee);
				cmd.Parameters.AddWithValue("@misc", model.MiscellaneousFee);
				cmd.Parameters.AddWithValue("@lab", model.LaboratoryFee);
				cmd.Parameters.AddWithValue("@units", model.Units);
				cmd.Parameters.AddWithValue("@id", model.CourseFeeID);
				con.Open();
				cmd.ExecuteNonQuery();
			}

			TempData["Success"] = "Course fee schedule updated.";
			return RedirectToAction("Index");
		}

		// ── DELETE ────────────────────────────────────────────────────
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Delete(int id)
		{
			if (Session["Role"] == null) return RedirectToAction("Login", "Account");

			using (var con = new SqlConnection(_conn))
			using (var cmd = new SqlCommand(
				"DELETE FROM CourseFees WHERE CourseFeeID = @id", con))
			{
				cmd.Parameters.AddWithValue("@id", id);
				con.Open();
				cmd.ExecuteNonQuery();
			}

			TempData["Success"] = "Fee schedule deleted.";
			return RedirectToAction("Index");
		}

		// ── HELPER ────────────────────────────────────────────────────
		private void PopulateCourses()
		{
			var courses = new List<SelectListItem>();
			using (var con = new SqlConnection(_conn))
			using (var cmd = new SqlCommand(
				"SELECT CourseID, CourseName FROM Courses ORDER BY CourseName", con))
			{
				con.Open();
				using (var r = cmd.ExecuteReader())
					while (r.Read())
						courses.Add(new SelectListItem
						{
							Value = r["CourseID"].ToString(),
							Text = r["CourseName"].ToString()
						});
			}
			ViewBag.Courses = courses;
			ViewBag.Semesters = new SelectList(new[] { "1st", "2nd", "Summer" });
		}
	}
}