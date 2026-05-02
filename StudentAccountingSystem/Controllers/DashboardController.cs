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

            int totalStudents = 0;
            int totalCourses = 0;
            int unpaidCount = 0;
            decimal totalPaid = 0;

            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Students WHERE Status = 'Enrolled'", con))
                    totalStudents = (int)cmd.ExecuteScalar();

                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Courses WHERE IsActive = 1", con))
                    totalCourses = (int)cmd.ExecuteScalar();

                using (var cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Assessments WHERE Status = 'Unpaid'", con))
                    unpaidCount = (int)cmd.ExecuteScalar();

                using (var cmd = new SqlCommand(
                    "SELECT ISNULL(SUM(AmountPaid), 0) FROM Payments", con))
                    totalPaid = (decimal)cmd.ExecuteScalar();
            }

            ViewBag.TotalStudents = totalStudents;
            ViewBag.TotalCourses = totalCourses;
            ViewBag.UnpaidCount = unpaidCount;
            ViewBag.TotalPaid = totalPaid;

            return View();
        }
    }
}