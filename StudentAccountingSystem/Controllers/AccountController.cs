using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace StudentAccountingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly string _conn = ConfigurationManager
            .ConnectionStrings["StudentAccountingDB"].ConnectionString;

        public ActionResult Login()
        {
            if (Session["Role"] != null)
                return RedirectToAction("Index", "Dashboard");
            return View();
        }

        public ActionResult DoLogin()
        {
            // 1. Create the list container like the sample
            List<object> data = new List<object>();

            string username = Request["username"];
            string password = Request["password"];

            // Validation
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                data.Add(new { success = false, message = "Please enter username and password." });
                return Json(data, JsonRequestBehavior.AllowGet);
            }

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT UserID, Role, StudentID, Username
          FROM   Users
          WHERE  Username     = @u
            AND  PasswordHash = @p
            AND  IsActive     = 1", con))
            {
                cmd.Parameters.AddWithValue("@u", username.Trim());
                cmd.Parameters.AddWithValue("@p", HashPassword(password.Trim()));
                con.Open();

                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        // Set Sessions
                        Session["UserID"] = (int)r["UserID"];
                        Session["Role"] = r["Role"].ToString();
                        Session["Username"] = r["Username"].ToString();

                        if (r["StudentID"] != DBNull.Value)
                            Session["StudentID"] = (int)r["StudentID"];

                        string redirect = r["Role"].ToString() == "Student"
                            ? "/StudentPortal/Index"
                            : "/Dashboard/Index";

                        // 2. Add the success object to the list
                        data.Add(new
                        {
                            success = true,
                            redirect = redirect,
                            mess = 1 // Adding "mess" to match your teacher's sample logic
                        });
                    }
                    else
                    {
                        // 3. Add the failure object to the list
                        data.Add(new
                        {
                            success = false,
                            message = "Invalid username or password.",
                            mess = 0
                        });
                    }
                }
            }

            // 4. Return the list (data) just like in ViewAllStudents
            return Json(data, JsonRequestBehavior.AllowGet);
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

        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }
    }
}