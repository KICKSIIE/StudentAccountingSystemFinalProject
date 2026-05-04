using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace StudentAccountingSystem.Controllers
{
    public class DiscountController : Controller
    {
        string connDB = ConfigurationManager.ConnectionStrings["StudentAccountingDB"].ConnectionString;

        public ActionResult Index() { return View(); }

        public ActionResult ViewAllDiscounts()
        {
            var list = new List<object>();
            using (var db = new SqlConnection(connDB))
            {
                db.Open();
                string sql = @"SELECT d.*, 
                      (SELECT COUNT(*) FROM Students s WHERE s.DiscountID = d.DiscountID) as StudentCount 
                      FROM Discounts d ORDER BY d.DiscountName";
                SqlCommand cmd = new SqlCommand(sql, db);
                SqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    list.Add(new
                    {
                        id = r["DiscountID"],
                        name = r["DiscountName"],
                        type = r["Type"],
                        percent = r["DiscountPercent"],
                        amount = r["DiscountAmount"],
                        enrolled = r["StudentCount"],
                        isActive = Convert.ToBoolean(r["IsActive"]) // ADD THIS
                    });
                }
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }

        // ADD ToggleDiscountActive()
        public ActionResult ToggleDiscountActive()
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
                    using (var cmd = new SqlCommand("SELECT IsActive FROM Discounts WHERE DiscountID=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        isActive = Convert.ToBoolean(cmd.ExecuteScalar());
                    }

                    // If trying to deactivate, check for students
                    if (isActive)
                    {
                        using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Students WHERE DiscountID=@id", con))
                        {
                            cmd.Parameters.AddWithValue("@id", id);
                            int count = (int)cmd.ExecuteScalar();
                            if (count > 0)
                            {
                                data.Add(new { mess = 0, message = "Cannot deactivate: " + count + " student(s) are using this discount." });
                                return Json(data, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }

                    // Safe to toggle
                    using (var cmd = new SqlCommand(
                        "UPDATE Discounts SET IsActive = CASE WHEN IsActive=1 THEN 0 ELSE 1 END WHERE DiscountID=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    // Return new status
                    using (var cmd = new SqlCommand("SELECT IsActive FROM Discounts WHERE DiscountID=@id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        bool newStatus = Convert.ToBoolean(cmd.ExecuteScalar());
                        data.Add(new { mess = 1, isActive = newStatus, message = newStatus ? "Discount activated." : "Discount deactivated." });
                    }
                }
            }
            catch
            {
                data.Add(new { mess = 0, message = "Could not update discount status." });
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }


        public ActionResult SaveDiscount()
        {
            using (var db = new SqlConnection(connDB))
            {
                db.Open();
                string sql = "INSERT INTO Discounts (DiscountName, Type, DiscountPercent, DiscountAmount, IsActive) VALUES (@n, @t, @p, @a, 1)";
                SqlCommand cmd = new SqlCommand(sql, db);
                cmd.Parameters.AddWithValue("@n", Request["name"]);
                cmd.Parameters.AddWithValue("@t", Request["type"]);
                cmd.Parameters.AddWithValue("@p", string.IsNullOrEmpty(Request["percent"]) ? (object)DBNull.Value : Request["percent"]);
                cmd.Parameters.AddWithValue("@a", string.IsNullOrEmpty(Request["amount"]) ? (object)DBNull.Value : Request["amount"]);
                cmd.ExecuteNonQuery();
            }
            return Json(new[] { new { mess = 1, message = "Discount Saved Successfully!" } });
        }

        public ActionResult GetDiscountByID(int id)
        {
            object data = null;
            using (var db = new SqlConnection(connDB))
            {
                db.Open();
                SqlCommand cmd = new SqlCommand("SELECT * FROM Discounts WHERE DiscountID = @id", db);
                cmd.Parameters.AddWithValue("@id", id);
                SqlDataReader r = cmd.ExecuteReader();
                if (r.Read())
                {
                    data = new
                    {
                        id = r["DiscountID"],
                        name = r["DiscountName"],
                        type = r["Type"],
                        percent = r["DiscountPercent"],
                        amount = r["DiscountAmount"]
                    };
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UpdateDiscount()
        {
            using (var db = new SqlConnection(connDB))
            {
                db.Open();
                string sql = "UPDATE Discounts SET DiscountName=@n, Type=@t, DiscountPercent=@p, DiscountAmount=@a WHERE DiscountID=@id";
                SqlCommand cmd = new SqlCommand(sql, db);
                cmd.Parameters.AddWithValue("@id", Request["id"]);
                cmd.Parameters.AddWithValue("@n", Request["name"]);
                cmd.Parameters.AddWithValue("@t", Request["type"]);
                cmd.Parameters.AddWithValue("@p", string.IsNullOrEmpty(Request["percent"]) ? (object)DBNull.Value : Request["percent"]);
                cmd.Parameters.AddWithValue("@a", string.IsNullOrEmpty(Request["amount"]) ? (object)DBNull.Value : Request["amount"]);
                cmd.ExecuteNonQuery();
            }
            return Json(new[] { new { mess = 1, message = "Discount Updated Successfully!" } });
        }

        public ActionResult DeleteDiscount()
        {
            string id = Request["id"];
            var data = new List<object>();

            try
            {
                using (var db = new SqlConnection(connDB))
                {
                    db.Open();

                    // 1. Check if active
                    using (var cmd = new SqlCommand("SELECT IsActive FROM Discounts WHERE DiscountID=@id", db))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        bool isActive = Convert.ToBoolean(cmd.ExecuteScalar());
                        if (isActive)
                        {
                            data.Add(new { mess = 0, message = "Cannot delete: discount is still active. Deactivate it first." });
                            return Json(data, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // 2. Check if students are using it
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Students WHERE DiscountID=@id", db))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        int count = (int)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            data.Add(new { mess = 0, message = "Cannot delete: " + count + " student(s) are using this discount." });
                            return Json(data, JsonRequestBehavior.AllowGet);
                        }
                    }

                    // 3. Safe to delete
                    using (var cmd = new SqlCommand("DELETE FROM Discounts WHERE DiscountID=@id", db))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    data.Add(new { mess = 1, message = "Discount deleted successfully." });
                }
            }
            catch
            {
                data.Add(new { mess = 0, message = "Could not delete discount." });
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }
    }
}