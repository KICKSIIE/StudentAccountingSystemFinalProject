using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using StudentAccountingSystem.Models;

namespace StudentAccountingSystem.Controllers
{
    public class DiscountController : Controller
    {
        private readonly string _conn = ConfigurationManager
            .ConnectionStrings["StudentAccountingDB"].ConnectionString;

        public ActionResult Index()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            var list = new List<Discount>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT DiscountID, DiscountName, Type, DiscountPercent, DiscountAmount, IsActive FROM Discounts ORDER BY DiscountName", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new Discount
                        {
                            DiscountID = (int)r["DiscountID"],
                            DiscountName = r["DiscountName"].ToString(),
                            Type = r["Type"].ToString(),
                            DiscountPercent = r["DiscountPercent"] == System.DBNull.Value ? (decimal?)null : (decimal)r["DiscountPercent"],
                            DiscountAmount = r["DiscountAmount"] == System.DBNull.Value ? (decimal?)null : (decimal)r["DiscountAmount"],
                            IsActive = (bool)r["IsActive"]
                        });
            }
            return View(list);
        }

        public ActionResult Create()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");
            return View(new Discount());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Discount model)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid) return View(model);

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"INSERT INTO Discounts (DiscountName, Type, DiscountPercent, DiscountAmount, IsActive)
                  VALUES (@name, @type, @percent, @amount, 1)", con))
            {
                cmd.Parameters.AddWithValue("@name", model.DiscountName.Trim());
                cmd.Parameters.AddWithValue("@type", model.Type);
                cmd.Parameters.AddWithValue("@percent", model.DiscountPercent.HasValue ? (object)model.DiscountPercent.Value : System.DBNull.Value);
                cmd.Parameters.AddWithValue("@amount", model.DiscountAmount.HasValue ? (object)model.DiscountAmount.Value : System.DBNull.Value);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            TempData["Success"] = "Discount added successfully.";
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            Discount model = null;
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT DiscountID, DiscountName, Type, DiscountPercent, DiscountAmount, IsActive FROM Discounts WHERE DiscountID = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                con.Open();
                using (var r = cmd.ExecuteReader())
                    if (r.Read())
                        model = new Discount
                        {
                            DiscountID = (int)r["DiscountID"],
                            DiscountName = r["DiscountName"].ToString(),
                            Type = r["Type"].ToString(),
                            DiscountPercent = r["DiscountPercent"] == System.DBNull.Value ? (decimal?)null : (decimal)r["DiscountPercent"],
                            DiscountAmount = r["DiscountAmount"] == System.DBNull.Value ? (decimal?)null : (decimal)r["DiscountAmount"],
                            IsActive = (bool)r["IsActive"]
                        };
            }
            if (model == null) return HttpNotFound();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Discount model)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid) return View(model);

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"UPDATE Discounts SET
                    DiscountName    = @name,
                    Type            = @type,
                    DiscountPercent = @percent,
                    DiscountAmount  = @amount,
                    IsActive        = @active
                  WHERE DiscountID = @id", con))
            {
                cmd.Parameters.AddWithValue("@name", model.DiscountName.Trim());
                cmd.Parameters.AddWithValue("@type", model.Type);
                cmd.Parameters.AddWithValue("@percent", model.DiscountPercent.HasValue ? (object)model.DiscountPercent.Value : System.DBNull.Value);
                cmd.Parameters.AddWithValue("@amount", model.DiscountAmount.HasValue ? (object)model.DiscountAmount.Value : System.DBNull.Value);
                cmd.Parameters.AddWithValue("@active", model.IsActive);
                cmd.Parameters.AddWithValue("@id", model.DiscountID);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            TempData["Success"] = "Discount updated successfully.";
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
                    "DELETE FROM Discounts WHERE DiscountID = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    con.Open();
                    cmd.ExecuteNonQuery();
                }
                TempData["Success"] = "Discount deleted successfully.";
            }
            catch
            {
                TempData["Error"] = "Cannot delete — discount is already assigned to a student.";
            }
            return RedirectToAction("Index");
        }
    }
}