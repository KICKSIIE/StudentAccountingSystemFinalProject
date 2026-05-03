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
                // SQL now counts students from the Students table
                string sql = @"SELECT c.*, 
                      (SELECT COUNT(*) FROM Students s WHERE s.CourseID = c.CourseID) as StudentCount 
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
                            enrolled = r["StudentCount"], // The count from SQL
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
                    // 1. Check if students are enrolled before trying to delete
                    string checkSql = "SELECT COUNT(*) FROM Students WHERE CourseID = @id";
                    using (var cmdCheck = new SqlCommand(checkSql, con))
                    {
                        cmdCheck.Parameters.AddWithValue("@id", id);
                        int count = (int)cmdCheck.ExecuteScalar();
                        if (count > 0)
                        {
                            data.Add(new { mess = 0, message = "Cannot delete: " + count + " student(s) currently enrolled." });
                            return Json(data, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // 2. If count is 0, proceed with delete
                    using (var cmd = new SqlCommand("DELETE FROM Courses WHERE CourseID = @id", con))
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
    }
}

