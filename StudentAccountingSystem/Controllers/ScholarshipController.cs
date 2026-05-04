using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace StudentAccountingSystem.Controllers
{
    public class ScholarshipController : Controller
    {
        string connDB = ConfigurationManager.ConnectionStrings["StudentAccountingDB"].ConnectionString;

        public ActionResult Index() { return View(); }

        public ActionResult ViewAllScholarships()
        {
            var list = new List<object>();
            using (var db = new SqlConnection(connDB))
            {
                db.Open();
                string sql = @"SELECT s.*, 
                      (SELECT COUNT(*) FROM Students st WHERE st.ScholarshipID = s.ScholarshipID) as EnrolledCount 
                      FROM Scholarships s ORDER BY s.ScholarshipName";
                SqlCommand cmd = new SqlCommand(sql, db);
                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new
                    {
                        id = r["ScholarshipID"],
                        name = r["ScholarshipName"],
                        type = r["Type"],
                        percent = r["DiscountPercent"],
                        amount = r["DiscountAmount"],
                        enrolled = r["EnrolledCount"],
                        isActive = Convert.ToBoolean(r["IsActive"]) // ADD THIS
                    });
                }
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ToggleScholarshipActive()
        {
            string id = Request["id"];
            var data = new List<object>();

            try
            {
                using (var con = new SqlConnection(connDB))
                {
                    con.Open();

                    // Get current status
                    bool isActive;
                    using (var cmd = new SqlCommand("SELECT IsActive FROM Scholarships WHERE ScholarshipID=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        isActive = Convert.ToBoolean(cmd.ExecuteScalar());
                    }

                    // If trying to deactivate, check for enrolled students
                    if (isActive)
                    {
                        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Students WHERE ScholarshipID=@id", con))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            int count = (int)cmd.ExecuteScalar();
                            if (count > 0)
                            {
                                data.Add(new { mess = 0, message = "Cannot deactivate: " + count + " student(s) are using this scholarship." });
                                return Json(data, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }

                    // Safe to toggle
                    using (var cmd = new SqlCommand(
                        "UPDATE Scholarships SET IsActive = CASE WHEN IsActive=1 THEN 0 ELSE 1 END WHERE ScholarshipID=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    // Return new status
                    using (var cmd = new SqlCommand("SELECT IsActive FROM Scholarships WHERE ScholarshipID=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        bool newStatus = Convert.ToBoolean(cmd.ExecuteScalar());
                        data.Add(new { mess = 1, isActive = newStatus, message = newStatus ? "Scholarship activated." : "Scholarship deactivated." });
                    }
                }
            }
            catch
            {
                data.Add(new { mess = 0, message = "Could not update scholarship status." });
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }


        public ActionResult SaveScholarship()
        {
            using (var db = new SqlConnection(connDB))
            {
                db.Open();
                string sql = "INSERT INTO Scholarships (ScholarshipName, Type, DiscountPercent, DiscountAmount, IsActive) VALUES (@n, @t, @p, @a, 1)";
                SqlCommand cmd = new SqlCommand(sql, db);
                cmd.Parameters.AddWithValue("@n", Request["name"]);
                cmd.Parameters.AddWithValue("@t", Request["type"]);
                cmd.Parameters.AddWithValue("@p", string.IsNullOrEmpty(Request["percent"]) ? (object)DBNull.Value : Request["percent"]);
                cmd.Parameters.AddWithValue("@a", string.IsNullOrEmpty(Request["amount"]) ? (object)DBNull.Value : Request["amount"]);
                cmd.ExecuteNonQuery();
            }
            return Json(new[] { new { mess = 1, message = "Saved Successfully!" } });
        }

        public ActionResult GetScholarshipByID(int id)
        {
            object data = null;
            using (var db = new SqlConnection(connDB))
            {
                db.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM Scholarships WHERE ScholarshipID = @id", db);
                cmd.Parameters.AddWithValue("@id", id);
                SqlDataReader r = cmd.ExecuteReader();
                if (r.Read())
                {
                    data = new
                    {
                        id = r["ScholarshipID"],
                        name = r["ScholarshipName"],
                        type = r["Type"],
                        percent = r["DiscountPercent"],
                        amount = r["DiscountAmount"]
                    };
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UpdateScholarship()
        {
            using (var db = new SqlConnection(connDB))
            {
                db.Open();
                string sql = "UPDATE Scholarships SET ScholarshipName=@n, Type=@t, DiscountPercent=@p, DiscountAmount=@a WHERE ScholarshipID=@id";
                SqlCommand cmd = new SqlCommand(sql, db);
                cmd.Parameters.AddWithValue("@id", Request["id"]);
                cmd.Parameters.AddWithValue("@n", Request["name"]);
                cmd.Parameters.AddWithValue("@t", Request["type"]);
                cmd.Parameters.AddWithValue("@p", string.IsNullOrEmpty(Request["percent"]) ? (object)DBNull.Value : Request["percent"]);
                cmd.Parameters.AddWithValue("@a", string.IsNullOrEmpty(Request["amount"]) ? (object)DBNull.Value : Request["amount"]);
                cmd.ExecuteNonQuery();
            }
            return Json(new[] { new { mess = 1, message = "Updated Successfully!" } });
        }

        public ActionResult DeleteScholarship()
        {
            string id = Request["id"];
            var data = new List<object>();

            try
            {
                using (var db = new SqlConnection(connDB))
                {
                    db.Open();

                    // 1. Check if active
                    using (var cmd = new SqlCommand("SELECT IsActive FROM Scholarships WHERE ScholarshipID=@id", db))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        bool isActive = Convert.ToBoolean(cmd.ExecuteScalar());
                        if (isActive)
                        {
                            data.Add(new { mess = 0, message = "Cannot delete: scholarship is still active. Deactivate it first." });
                            return Json(data, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // 2. Check if students are using it
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Students WHERE ScholarshipID=@id", db))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        int count = (int)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            data.Add(new { mess = 0, message = "Cannot delete: " + count + " student(s) are using this scholarship." });
                            return Json(data, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // 3. Safe to delete
                    using (var cmd = new SqlCommand("DELETE FROM Scholarships WHERE ScholarshipID=@id", db))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    data.Add(new { mess = 1, message = "Deleted successfully." });
                }
            }
            catch
            {
                data.Add(new { mess = 0, message = "Could not delete scholarship." });
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }
    }
}