using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.Web.Mvc;

namespace StudentAccountingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly string _conn = ConfigurationManager
            .ConnectionStrings["StudentAccountingDB"].ConnectionString;

        // GET: /Account/Login
        public ActionResult Login()
        {
            if (Session["Role"] != null)
                return RedirectToAction("Index", "Dashboard");
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
        SELECT u.UserID, u.Role, u.StudentID, u.Username
        FROM Users u
        WHERE u.Username     = @u
          AND u.PasswordHash = @p
          AND u.IsActive     = 1", con))
            {
                cmd.Parameters.AddWithValue("@u", username.Trim());
                cmd.Parameters.AddWithValue("@p", password.Trim());
                con.Open();

                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        Session["UserID"] = (int)r["UserID"];
                        Session["Role"] = r["Role"].ToString();
                        Session["Username"] = r["Username"].ToString();

                        // If student, store StudentID in session
                        if (r["StudentID"] != DBNull.Value)
                            Session["StudentID"] = (int)r["StudentID"];

                        if (r["Role"].ToString() == "Student")
                            return RedirectToAction("Index", "StudentPortal");
                        else
                            return RedirectToAction("Index", "Dashboard");
                    }
                    else
                    {
                        ViewBag.Error = "Invalid username or password.";
                        return View();
                    }
                }
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }
    }
}