using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace StudentAccountingSystem.Controllers
{
    public class ReactivationController : Controller
    {
        private readonly string _conn = ConfigurationManager
            .ConnectionStrings["StudentAccountingDB"].ConnectionString;

        // GET: /Reactivation/ — Admin sees all requests
        public ActionResult Index()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            var list = new List<Models.ReactivationRequest>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT r.RequestID, r.StudentID, r.RequestDate, r.Status,
                         r.TargetSchoolYear, r.TargetSemester,
                         r.ProcessedDate, r.Remarks,
                         s.StudentNumber,
                         s.FirstName + ' ' + s.LastName AS StudentName,
                         s.YearLevel, s.Semester, s.SchoolYear,
                         c.CourseName
                  FROM   ReactivationRequests r
                  JOIN   Students s ON s.StudentID = r.StudentID
                  JOIN   Courses  c ON c.CourseID  = s.CourseID
                  ORDER  BY r.RequestDate DESC", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new Models.ReactivationRequest
                        {
                            RequestID        = (int)r["RequestID"],
                            StudentID        = (int)r["StudentID"],
                            StudentNumber    = r["StudentNumber"].ToString(),
                            StudentName      = r["StudentName"].ToString(),
                            CourseName       = r["CourseName"].ToString(),
                            YearLevel        = (int)r["YearLevel"],
                            OldSemester      = r["Semester"].ToString(),
                            OldSchoolYear    = r["SchoolYear"].ToString(),
                            RequestDate      = (DateTime)r["RequestDate"],
                            Status           = r["Status"].ToString(),
                            TargetSchoolYear = r["TargetSchoolYear"].ToString(),
                            TargetSemester   = r["TargetSemester"].ToString(),
                            ProcessedDate    = r["ProcessedDate"] == DBNull.Value
                                              ? (DateTime?)null
                                              : (DateTime)r["ProcessedDate"],
                            Remarks          = r["Remarks"] == DBNull.Value
                                              ? "" : r["Remarks"].ToString()
                        });
            }
            return View(list);
        }

        // POST: /Reactivation/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            int adminID = (int)Session["UserID"];

            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                // Get request details
                int studentID = 0;
                string targetSchoolYear = "";
                string targetSemester = "";
                int courseID = 0;
                int yearLevel = 0;
                string oldSemester = "";
                int targetYearLevel = 1; // ← ADD THIS LINE

                using (var cmd = new SqlCommand(
                    @"SELECT r.StudentID, r.TargetSchoolYear, r.TargetSemester,
                             r.TargetYearLevel,
                             s.CourseID, s.YearLevel, s.Semester
                      FROM   ReactivationRequests r
                      JOIN   Students s ON s.StudentID = r.StudentID
                      WHERE  r.RequestID = @rid AND r.Status = 'Pending'", con))
                {
                    cmd.Parameters.AddWithValue("@rid", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                        {
                            TempData["Error"] = "Request not found or already processed.";
                            return RedirectToAction("Index");
                        }
                        targetYearLevel = (int)r["TargetYearLevel"];
                        studentID        = (int)r["StudentID"];
                        targetSchoolYear = r["TargetSchoolYear"].ToString();
                        targetSemester   = r["TargetSemester"].ToString();
                        courseID         = (int)r["CourseID"];
                        yearLevel        = (int)r["YearLevel"];
                        oldSemester      = r["Semester"].ToString();
                    }
                }

                // Determine new year level
                // If they stopped at 1st sem and coming back to 1st sem
                // year level stays same, only school year advances
                // If they stopped at 1st sem and coming back to 2nd sem
                // that means they skipped — keep same year level
                int newYearLevel = targetYearLevel; // use pre-calculated value from request

                // If the student stopped mid-year (withdrew at 1st sem)
                // and is coming back at 1st sem of new year,
                // they repeat the same year level
                // If they withdrew at 2nd sem, coming back at 1st sem next year
                // they advance year level  
                // Cap at year 4
                if (newYearLevel > 4) newYearLevel = 4;

                // Update student record
                using (var cmd = new SqlCommand(
                    @"UPDATE Students SET
                        Status     = 'Enrolled',
                        YearLevel  = @year,
                        Semester   = @sem,
                        SchoolYear = @sy
                      WHERE StudentID = @sid", con))
                {
                    cmd.Parameters.AddWithValue("@year", newYearLevel);
                    cmd.Parameters.AddWithValue("@sem",  targetSemester);
                    cmd.Parameters.AddWithValue("@sy",   targetSchoolYear);
                    cmd.Parameters.AddWithValue("@sid",  studentID);
                    cmd.ExecuteNonQuery();
                }

                // Auto-generate 4 new assessments for new semester
                StudentController.GenerateAssessments(con, studentID, courseID,
                    newYearLevel, targetSemester, targetSchoolYear);

                // Mark request as Approved
                using (var cmd = new SqlCommand(
                    @"UPDATE ReactivationRequests SET
                        Status        = 'Approved',
                        ProcessedBy   = @admin,
                        ProcessedDate = GETDATE()
                      WHERE RequestID = @rid", con))
                {
                    cmd.Parameters.AddWithValue("@admin", adminID);
                    cmd.Parameters.AddWithValue("@rid",   id);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Success"] = "Reactivation approved. Student is now Enrolled with new assessments.";
            return RedirectToAction("Index");
        }

        // POST: /Reactivation/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            int adminID = (int)Session["UserID"];

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"UPDATE ReactivationRequests SET
                    Status        = 'Rejected',
                    ProcessedBy   = @admin,
                    ProcessedDate = GETDATE()
                  WHERE RequestID = @rid AND Status = 'Pending'", con))
            {
                cmd.Parameters.AddWithValue("@admin", adminID);
                cmd.Parameters.AddWithValue("@rid",   id);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["Success"] = "Reactivation request rejected.";
            return RedirectToAction("Index");
        }
    }
}