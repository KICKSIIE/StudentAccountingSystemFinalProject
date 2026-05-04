using StudentAccountingSystem.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace StudentAccountingSystem.Controllers
{
    public class CourseController : Controller
    {
        string connDB = ConfigurationManager.ConnectionStrings["StudentAccountingDB"].ConnectionString;

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ViewAllCourses()
        {
            var list = new List<object>();
            using (var db = new SqlConnection(connDB))
            {
                db.Open();
                // MODIFIED: Only count students who are NOT Graduated
                string sql = @"SELECT c.*, 
              (SELECT COUNT(*) FROM Students s 
               WHERE s.CourseID = c.CourseID 
                 AND s.Status != 'Graduated'  -- ADD THIS LINE
                 AND s.Status != 'Dropped'    -- OPTIONAL: exclude dropped too
              ) as StudentCount 
              FROM Courses c";

                using (var cmd = new SqlCommand(sql, db))
                {
                    SqlDataReader r = cmd.ExecuteReader();
                    while (r.Read())
                    {
                        list.Add(new
                        {
                            id = r["CourseID"],
                            code = r["CourseCode"],
                            name = r["CourseName"],
                            units = r["Units"],
                            tpu = r["TuitionPerUnit"],
                            reg = r["RegistrationFee"],
                            misc = r["MiscFee"],
                            lab = r["LabFee"],
                            isActive = Convert.ToBoolean(r["IsActive"]),
                            enrolled = r["StudentCount"],
                            totalFee = (Convert.ToDecimal(r["TuitionPerUnit"]) * Convert.ToInt32(r["Units"]) +
                                Convert.ToDecimal(r["RegistrationFee"]) +
                                Convert.ToDecimal(r["MiscFee"]) +
                                Convert.ToDecimal(r["LabFee"])).ToString("N2")
                        });
                    }
                }
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public ActionResult SaveCourse()
        {
            using (var db = new SqlConnection(connDB))
            {
                db.Open();
                string sql = "INSERT INTO Courses (CourseCode, CourseName, Units, TuitionPerUnit, RegistrationFee, MiscFee, LabFee, IsActive) " +
                             "VALUES (@code, @name, @units, @tpu, @reg, @misc, @lab, 1)";

                using (var cmd = new SqlCommand(sql, db))
                {
                    cmd.Parameters.AddWithValue("@code", Request["code"]);
                    cmd.Parameters.AddWithValue("@name", Request["name"]);
                    cmd.Parameters.AddWithValue("@units", Request["units"]);
                    cmd.Parameters.AddWithValue("@tpu", Request["tpu"]);
                    cmd.Parameters.AddWithValue("@reg", Request["reg"]);
                    cmd.Parameters.AddWithValue("@misc", Request["misc"]);
                    cmd.Parameters.AddWithValue("@lab", Request["lab"]);

                    cmd.ExecuteNonQuery();
                }
            }
            return Json(new { message = "Saved!" }, JsonRequestBehavior.AllowGet);
        }

        // Get a single course to fill the Edit form
        public ActionResult GetCourseByID(int id)
        {
            object data = null;
            using (var db = new SqlConnection(connDB))
            {
                db.Open();
                string sql = "SELECT * FROM Courses WHERE CourseID = @id";
                using (var cmd = new SqlCommand(sql, db))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    SqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        data = new
                        {
                            id = r["CourseID"],
                            code = r["CourseCode"],
                            name = r["CourseName"],
                            units = r["Units"],
                            tpu = r["TuitionPerUnit"],
                            reg = r["RegistrationFee"],
                            misc = r["MiscFee"],
                            lab = r["LabFee"]
                        };
                    }
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // Update the record in DB
        public ActionResult UpdateCourse()
        {
            using (var db = new SqlConnection(connDB))
            {
                db.Open();
                string sql = @"UPDATE Courses SET 
                        CourseCode=@code, CourseName=@name, Units=@units, 
                        TuitionPerUnit=@tpu, RegistrationFee=@reg, 
                        MiscFee=@misc, LabFee=@lab 
                      WHERE CourseID=@id";

                using (var cmd = new SqlCommand(sql, db))
                {
                    cmd.Parameters.AddWithValue("@id", Request["courseID"]);
                    cmd.Parameters.AddWithValue("@code", Request["code"]);
                    cmd.Parameters.AddWithValue("@name", Request["name"]);
                    cmd.Parameters.AddWithValue("@units", Request["units"]);
                    cmd.Parameters.AddWithValue("@tpu", Request["tpu"]);
                    cmd.Parameters.AddWithValue("@reg", Request["reg"]);
                    cmd.Parameters.AddWithValue("@misc", Request["misc"]);
                    cmd.Parameters.AddWithValue("@lab", Request["lab"]);
                    cmd.ExecuteNonQuery();
                }
            }
            return Json(new { message = "Course Updated Successfully!" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DeleteCourse()
        {
            string id = Request["id"];
            var data = new List<object>();

            try
            {
                using (var con = new SqlConnection(connDB))
                {
                    con.Open();

                    // 1. Check if active first
                    using (var cmd = new SqlCommand("SELECT IsActive FROM Courses WHERE CourseID=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        bool isActive = Convert.ToBoolean(cmd.ExecuteScalar());
                        if (isActive)
                        {
                            data.Add(new { mess = 0, message = "Cannot delete: course is still active. Deactivate it first." });
                            return Json(data, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // 2. Check if students are enrolled (excluding Graduated)
                    using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM Students 
                                              WHERE CourseID = @id 
                                                AND Status != 'Graduated'
                                                AND Status != 'Dropped'", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        int count = (int)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            data.Add(new { mess = 0, message = "Cannot delete: " + count + " student(s) still enrolled in this course." });
                            return Json(data, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // 3. Safe to delete
                    using (var cmd = new SqlCommand("DELETE FROM Courses WHERE CourseID=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                data.Add(new { mess = 1, message = "Course deleted successfully." });
            }
            catch
            {
                data.Add(new { mess = 0, message = "Could not delete course due to a database error." });
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ToggleCourseActive()
        {
            string id = Request["id"];
            var data = new List<object>();

            try
            {
                using (var con = new SqlConnection(connDB))
                {
                    con.Open();

                    // Get current status first
                    bool isActive;
                    using (var cmd = new SqlCommand("SELECT IsActive FROM Courses WHERE CourseID=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        isActive = Convert.ToBoolean(cmd.ExecuteScalar());
                    }

                    // If trying to DEACTIVATE, check for enrolled students (excluding Graduated)
                    if (isActive)
                    {
                        using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM Students 
                                                  WHERE CourseID = @id 
                                                    AND Status != 'Graduated'
                                                    AND Status != 'Dropped'", con))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            int count = (int)cmd.ExecuteScalar();
                            if (count > 0)
                            {
                                data.Add(new { mess = 0, message = "Cannot deactivate: " + count + " student(s) still enrolled in this course." });
                                return Json(data, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }

                    // Safe to toggle
                    using (var cmd = new SqlCommand(
                        "UPDATE Courses SET IsActive = CASE WHEN IsActive=1 THEN 0 ELSE 1 END WHERE CourseID=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    // Return new status
                    using (var cmd = new SqlCommand("SELECT IsActive FROM Courses WHERE CourseID=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        bool newStatus = Convert.ToBoolean(cmd.ExecuteScalar());
                        data.Add(new { mess = 1, isActive = newStatus, message = newStatus ? "Course activated." : "Course deactivated." });
                    }
                }
            }
            catch
            {
                data.Add(new { mess = 0, message = "Could not update course status." });
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Create()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            return View();
        }

        public ActionResult Edit(int id)
        {
            Models.Course course = null;
            using (var db = new SqlConnection(connDB))
            {
                db.Open();
                string sql = "SELECT * FROM Courses WHERE CourseID = @id";
                using (var cmd = new SqlCommand(sql, db))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    SqlDataReader r = cmd.ExecuteReader();
                    if (r.Read())
                    {
                        course = new Models.Course
                        {
                            CourseID = Convert.ToInt32(r["CourseID"]),
                            CourseCode = r["CourseCode"].ToString(),
                            CourseName = r["CourseName"].ToString(),
                            Units = Convert.ToInt32(r["Units"]),
                            TuitionPerUnit = Convert.ToDecimal(r["TuitionPerUnit"]),
                            RegistrationFee = Convert.ToDecimal(r["RegistrationFee"]),
                            MiscFee = Convert.ToDecimal(r["MiscFee"]),
                            LabFee = Convert.ToDecimal(r["LabFee"])
                        };
                    }
                }
            }

            if (course == null) return HttpNotFound();
            return View(course);
        }
    }
}

