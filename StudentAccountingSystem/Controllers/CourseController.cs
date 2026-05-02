using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using StudentAccountingSystem.Models;

namespace StudentAccountingSystem.Controllers
{
    public class CourseController : Controller
    {
        private readonly string _conn = ConfigurationManager
            .ConnectionStrings["StudentAccountingDB"].ConnectionString;

        public ActionResult Index()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            var list = new List<Course>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT CourseID, CourseCode, CourseName, IsActive,
                         Units, TuitionPerUnit, RegistrationFee, MiscFee, LabFee
                  FROM   Courses ORDER BY CourseCode", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new Course
                        {
                            CourseID = (int)r["CourseID"],
                            CourseCode = r["CourseCode"].ToString(),
                            CourseName = r["CourseName"].ToString(),
                            IsActive = (bool)r["IsActive"],
                            Units = (int)r["Units"],
                            TuitionPerUnit = (decimal)r["TuitionPerUnit"],
                            RegistrationFee = (decimal)r["RegistrationFee"],
                            MiscFee = (decimal)r["MiscFee"],
                            LabFee = (decimal)r["LabFee"]
                        });
            }
            return View(list);
        }

        public ActionResult Create()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");
            return View(new Course());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Course model)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid) return View(model);

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"INSERT INTO Courses
                    (CourseCode, CourseName, IsActive,
                     Units, TuitionPerUnit, RegistrationFee, MiscFee, LabFee)
                  VALUES
                    (@code, @name, 1,
                     @units, @tpu, @reg, @misc, @lab)", con))
            {
                cmd.Parameters.AddWithValue("@code", model.CourseCode.Trim().ToUpper());
                cmd.Parameters.AddWithValue("@name", model.CourseName.Trim());
                cmd.Parameters.AddWithValue("@units", model.Units);
                cmd.Parameters.AddWithValue("@tpu", model.TuitionPerUnit);
                cmd.Parameters.AddWithValue("@reg", model.RegistrationFee);
                cmd.Parameters.AddWithValue("@misc", model.MiscFee);
                cmd.Parameters.AddWithValue("@lab", model.LabFee);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            TempData["Success"] = "Course added successfully.";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            Course model = null;
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT CourseID, CourseCode, CourseName, IsActive,
                         Units, TuitionPerUnit, RegistrationFee, MiscFee, LabFee
                  FROM   Courses WHERE CourseID = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                con.Open();
                using (var r = cmd.ExecuteReader())
                    if (r.Read())
                        model = new Course
                        {
                            CourseID = (int)r["CourseID"],
                            CourseCode = r["CourseCode"].ToString(),
                            CourseName = r["CourseName"].ToString(),
                            IsActive = (bool)r["IsActive"],
                            Units = (int)r["Units"],
                            TuitionPerUnit = (decimal)r["TuitionPerUnit"],
                            RegistrationFee = (decimal)r["RegistrationFee"],
                            MiscFee = (decimal)r["MiscFee"],
                            LabFee = (decimal)r["LabFee"]
                        };
            }
            if (model == null) return HttpNotFound();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Course model)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid) return View(model);

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"UPDATE Courses SET
                    CourseCode      = @code,
                    CourseName      = @name,
                    IsActive        = @active,
                    Units           = @units,
                    TuitionPerUnit  = @tpu,
                    RegistrationFee = @reg,
                    MiscFee         = @misc,
                    LabFee          = @lab
                  WHERE CourseID = @id", con))
            {
                cmd.Parameters.AddWithValue("@code", model.CourseCode.Trim().ToUpper());
                cmd.Parameters.AddWithValue("@name", model.CourseName.Trim());
                cmd.Parameters.AddWithValue("@active", model.IsActive);
                cmd.Parameters.AddWithValue("@units", model.Units);
                cmd.Parameters.AddWithValue("@tpu", model.TuitionPerUnit);
                cmd.Parameters.AddWithValue("@reg", model.RegistrationFee);
                cmd.Parameters.AddWithValue("@misc", model.MiscFee);
                cmd.Parameters.AddWithValue("@lab", model.LabFee);
                cmd.Parameters.AddWithValue("@id", model.CourseID);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            TempData["Success"] = "Course updated successfully.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");
            try
            {
                using (var con = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(
                    "DELETE FROM Courses WHERE CourseID = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["Success"] = "Course deleted.";
            }
            catch
            {
                TempData["Error"] = "Cannot delete — course is assigned to a student.";
            }
            return RedirectToAction("Index");
        }
    }
}