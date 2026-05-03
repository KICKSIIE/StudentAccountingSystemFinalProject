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
                // SQL query includes count of students currently assigned this discount
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
                        enrolled = r["StudentCount"]
                    });
                }
            }
            return Json(list, JsonRequestBehavior.AllowGet);
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
            using (var db = new SqlConnection(connDB))
            {
                db.Open();
                // Check if students are enrolled using this discount
                SqlCommand check = new SqlCommand("SELECT COUNT(*) FROM Students WHERE DiscountID = @id", db);
                check.Parameters.AddWithValue("@id", id);
                int count = (int)check.ExecuteScalar();

                if (count > 0)
                {
                    data.Add(new { mess = 0, message = "Cannot delete: " + count + " student(s) are currently using this discount." });
                }
                else
                {
                    SqlCommand cmd = new SqlCommand("DELETE FROM Discounts WHERE DiscountID = @id", db);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                    data.Add(new { mess = 1, message = "Discount deleted successfully." });
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }
    }
}