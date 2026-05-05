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
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");
            return View();
        }

        public JsonResult GetDashboardData()
        {
            var data = new List<object>();

            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                // Financial summary
                decimal totalAssessed = 0, totalCollected = 0, totalRefunded = 0;
                using (var cmd = new SqlCommand(@"
                    SELECT
                        ISNULL((SELECT SUM(AmountDue) FROM Assessments), 0) AS TotalAssessed,
                        ISNULL((SELECT SUM(AmountPaid) FROM Payments WHERE TransactionType = 'Payment'), 0) AS TotalCollected,
                        ISNULL((SELECT SUM(AmountPaid) FROM Payments WHERE TransactionType = 'Refund'), 0) AS TotalRefunded", con))
                using (var r = cmd.ExecuteReader())
                    if (r.Read())
                    {
                        totalAssessed = Convert.ToDecimal(r["TotalAssessed"]);
                        totalCollected = Convert.ToDecimal(r["TotalCollected"]);
                        totalRefunded = Convert.ToDecimal(r["TotalRefunded"]);
                    }

                // Student counts by status
                int totalRegistered = 0, totalEnrolled = 0, totalGraduated = 0;
                using (var cmd = new SqlCommand(@"
                    SELECT Status, COUNT(*) AS Count
                    FROM Students
                    GROUP BY Status", con))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                    {
                        int count = Convert.ToInt32(r["Count"]);
                        totalRegistered += count;
                        switch (r["Status"].ToString())
                        {
                            case "Enrolled": totalEnrolled = count; break;
                            case "Graduated": totalGraduated = count; break;
                        }
                    }

                // Active courses
                int totalCourses = 0;
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Courses WHERE IsActive = 1", con))
                    totalCourses = (int)cmd.ExecuteScalar();

                // Unpaid assessment count
                int unpaidCount = 0;
                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Assessments WHERE Status = 'Unpaid'", con))
                    unpaidCount = (int)cmd.ExecuteScalar();

                data.Add(new
                {
                    mess = 1,
                    // Financial
                    totalAssessed = totalAssessed.ToString("N2"),
                    totalCollected = totalCollected.ToString("N2"),
                    outstandingBal = (totalAssessed - totalCollected).ToString("N2"),
                    totalRefunded = totalRefunded.ToString("N2"),
                    // Students
                    totalRegistered = totalRegistered,
                    totalEnrolled = totalEnrolled,
                    totalGraduated = totalGraduated,

                    // Other
                    totalCourses = totalCourses,
                    unpaidCount = unpaidCount
                });
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }
    }
}