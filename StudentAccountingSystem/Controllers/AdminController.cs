using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

namespace StudentAccountingSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly string _conn = ConfigurationManager
            .ConnectionStrings["StudentAccountingDB"].ConnectionString;

        public ActionResult Index()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");
            return View();
        }

        public ActionResult CreateAdmin(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return Json(new { mess = 0, message = "Username and password are required." });

                var hash = HashPassword(password);

                using (var con = new SqlConnection(_conn))
                {
                    con.Open();

                    // Check if username already exists
                    using (var check = new SqlCommand(
                        "SELECT COUNT(*) FROM Users WHERE Username = @u", con))
                    {
                        check.Parameters.AddWithValue("@u", username);
                        int exists = (int)check.ExecuteScalar();
                        if (exists > 0)
                            return Json(new { mess = 0, message = "Username already exists." });
                    }

                    // Insert new admin
                    int newUserID = 0;
                    using (var cmd = new SqlCommand(@"
                        INSERT INTO Users (Username, PasswordHash, Role, IsActive, CreatedAt)
                        VALUES (@u, @p, 'Admin', 1, GETDATE());
                        SELECT SCOPE_IDENTITY();", con))
                    {
                        cmd.Parameters.AddWithValue("@u", username);
                        cmd.Parameters.AddWithValue("@p", hash);
                        newUserID = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    return Json(new { mess = 1, message = "Admin account created successfully.", userID = newUserID });
                }
            }
            catch (Exception ex)
            {
                return Json(new { mess = 0, message = ex.Message });
            }
        }

        public ActionResult GetAdmins()
        {
            try
            {
                var list = new System.Collections.Generic.List<object>();
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();
                    using (var cmd = new SqlCommand(@"
                        SELECT UserID, Username, IsActive, CreatedAt
                        FROM Users
                        WHERE Role = 'Admin'
                        ORDER BY CreatedAt DESC", con))
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new
                            {
                                userID = r["UserID"],
                                username = r["Username"],
                                isActive = r["IsActive"],
                                createdAt = ((DateTime)r["CreatedAt"]).ToString("MMM dd, yyyy")
                            });
                }
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { mess = 0, message = ex.Message });
            }
        }

        public ActionResult ToggleStatus(int userID)
        {
            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();
                    using (var cmd = new SqlCommand(@"
                        UPDATE Users SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END
                        WHERE UserID = @id AND Role = 'Admin'", con))
                    {
                        cmd.Parameters.AddWithValue("@id", userID);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { mess = 1, message = "Status updated." });
            }
            catch (Exception ex)
            {
                return Json(new { mess = 0, message = ex.Message });
            }
        }

        public ActionResult ResetPassword(int userID, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword))
                    return Json(new { mess = 0, message = "Password cannot be empty." });

                var hash = HashPassword(newPassword);
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();
                    using (var cmd = new SqlCommand(@"
                        UPDATE Users SET PasswordHash = @p
                        WHERE UserID = @id AND Role = 'Admin'", con))
                    {
                        cmd.Parameters.AddWithValue("@p", hash);
                        cmd.Parameters.AddWithValue("@id", userID);
                        cmd.ExecuteNonQuery();
                    }
                }
                return Json(new { mess = 1, message = "Password reset successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { mess = 0, message = ex.Message });
            }
        }

        private string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder();
                foreach (var b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}