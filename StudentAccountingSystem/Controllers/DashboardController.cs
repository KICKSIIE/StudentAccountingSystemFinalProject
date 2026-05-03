using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace StudentAccountingSystem.Controllers
{
    public class DashboardController : Controller
    {
        private readonly string _conn = ConfigurationManager
            .ConnectionStrings["StudentAccountingDB"].ConnectionString;

        public ActionResult Index()
        {
            // Keep the security check
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            return View();
        }

        public JsonResult GetDashboardData()
        {
            List<object> data = new List<object>();
            int totalStudents = 0;
            int totalCourses = 0;
            int unpaidCount = 0;
            decimal totalPaid = 0;

            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                // Total Students
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Students WHERE Status = 'Enrolled'", con))
                    totalStudents = (int)cmd.ExecuteScalar();

                // Total Courses
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Courses WHERE IsActive = 1", con))
                    totalCourses = (int)cmd.ExecuteScalar();

                // Unpaid
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Assessments WHERE Status = 'Unpaid'", con))
                    unpaidCount = (int)cmd.ExecuteScalar();

                // Total Collections
                using (var cmd = new SqlCommand("SELECT ISNULL(SUM(AmountPaid), 0) FROM Payments", con))
                    totalPaid = Convert.ToDecimal(cmd.ExecuteScalar());
            }

            // Add to list following teacher's "data.Add" sample
            data.Add(new
            {
                totalStudents = totalStudents,
                totalCourses = totalCourses,
                unpaidCount = unpaidCount,
                totalPaid = totalPaid.ToString("N2"),
                mess = 1
            });

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAdminDashboardStats()
        {
            List<object> data = new List<object>();

            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                string query = @"
    SELECT 
        ISNULL(SUM(a.AmountDue), 0) as TotalAssessed,
        ISNULL((SELECT SUM(p.AmountPaid) FROM Payments p WHERE p.TransactionType = 'Payment'), 0) as TotalPaid,
        ISNULL(SUM(a.AmountDue), 0) - ISNULL((SELECT SUM(p.AmountPaid) FROM Payments p WHERE p.TransactionType = 'Payment'), 0) as TotalBalance
    FROM Assessments a";

                using (var cmd = new SqlCommand(query, con))
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        data.Add(new
                        {
                            totalAssessed = Convert.ToDecimal(r["TotalAssessed"]).ToString("N2"),
                            totalPaid = Convert.ToDecimal(r["TotalPaid"]).ToString("N2"),
                            totalBalance = Convert.ToDecimal(r["TotalBalance"]).ToString("N2"),
                            mess = 1
                        });
                    }
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }
    }
}