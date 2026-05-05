using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace StudentAccountingSystem.Controllers
{
    public class ReportController : Controller
    {
        private readonly string _conn = ConfigurationManager
            .ConnectionStrings["StudentAccountingDB"].ConnectionString;

        public ActionResult Index()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");
            return View();
        }

        // ── STUDENT LEDGER ────────────────────────────────────────────
        public ActionResult StudentLedger()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            // Use SelectListItem instead of anonymous object
            var students = new List<SelectListItem>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT StudentID, LastName + ', ' + FirstName + ' ' + ISNULL(MiddleName, '') AS FullName
                  FROM Students ORDER BY LastName", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        students.Add(new SelectListItem
                        {
                            Value = r["StudentID"].ToString(),
                            Text = r["StudentID"].ToString() + " — " + r["FullName"].ToString()
                        });
            }
            ViewBag.Students = students;
            return View();
        }

        public ActionResult GetStudentLedger(int studentID)
        {
            var data = new List<object>();
            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();

                    // 1. Student header info
                    object studentInfo = null;
                    using (var cmd = new SqlCommand(@"
                        SELECT s.StudentID, s.FirstName, s.LastName, s.MiddleName,
                               s.YearLevel, s.Semester, s.SchoolYear,
                               s.Status, c.CourseName, c.CourseCode,
                               sc.ScholarshipName, sc.Type AS SchType,
                               sc.DiscountPercent AS SchPct,
                               sc.DiscountAmount AS SchAmt,
                               d.DiscountName, d.Type AS DisType,
                               d.DiscountPercent AS DisPct,
                               d.DiscountAmount AS DisAmt
                        FROM Students s
                        LEFT JOIN Courses c ON c.CourseID = s.CourseID
                        LEFT JOIN Scholarships sc ON sc.ScholarshipID = s.ScholarshipID
                        LEFT JOIN Discounts d ON d.DiscountID = s.DiscountID
                        WHERE s.StudentID = @sid", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        using (var r = cmd.ExecuteReader())
                            if (r.Read())
                                studentInfo = new
                                {
                                    studentID = r["StudentID"],
                                    fullName = r["LastName"].ToString() + ", " + r["FirstName"].ToString() + " " + (r["MiddleName"] == DBNull.Value ? "" : r["MiddleName"].ToString()),
                                    course = r["CourseCode"].ToString() + " - " + r["CourseName"].ToString(),
                                    yearLevel = r["YearLevel"],
                                    semester = r["Semester"],
                                    schoolYear = r["SchoolYear"],
                                    status = r["Status"],
                                    scholarship = r["ScholarshipName"] == DBNull.Value ? "None" : r["ScholarshipName"].ToString(),
                                    discount = r["DiscountName"] == DBNull.Value ? "None" : r["DiscountName"].ToString()
                                };
                    }

                    // 2. All assessments (charges)
                    var assessments = new List<object>();
                    using (var cmd = new SqlCommand(@"
                        SELECT a.AssessmentID, a.Period, a.SchoolYear,
                               a.Semester, a.YearLevel,
                               a.TuitionFee, a.RegistrationFee, a.MiscFee, a.LabFee,
c.Units, c.TuitionPerUnit,
                               a.TotalFee, a.DiscountAmount, a.NetAssessment,
                               a.PercentRequired, a.AmountDue, 
                               a.Status, a.AssessedAt, a.OverpaidAmount,
                               ISNULL(SUM(p.AmountPaid), 0) AS TotalPaid
                        FROM Assessments a
                        LEFT JOIN Payments p ON p.AssessmentID = a.AssessmentID
                                            AND p.TransactionType = 'Payment'
                        JOIN Students s ON s.StudentID = a.StudentID
JOIN Courses c ON c.CourseID = s.CourseID
WHERE a.StudentID = @sid
GROUP BY a.AssessmentID, a.Period, a.SchoolYear,
         a.Semester, a.YearLevel,
         a.TuitionFee, a.RegistrationFee, a.MiscFee, a.LabFee,
         c.Units, c.TuitionPerUnit,
                                 a.TotalFee, a.DiscountAmount, a.NetAssessment,
                                 a.PercentRequired, a.AmountDue,
                                 a.Status, a.AssessedAt, a.OverpaidAmount
                        ORDER BY a.YearLevel, a.SchoolYear, a.Semester,
                                 CASE a.Period
                                     WHEN 'Prelim'     THEN 1
                                     WHEN 'Midterm'    THEN 2
                                     WHEN 'Semi-Final' THEN 3
                                     WHEN 'Final'      THEN 4
                                 END", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                assessments.Add(new
                                {
                                    assessmentID = r["AssessmentID"],
                                    period = r["Period"],
                                    schoolYear = r["SchoolYear"],
                                    semester = r["Semester"],
                                    yearLevel = r["YearLevel"],
                                    units = r["Units"],
                                    tuitionPerUnit = r["TuitionPerUnit"],
                                    tuitionFee = r["TuitionFee"],
                                    registrationFee = r["RegistrationFee"],
                                    miscFee = r["MiscFee"],
                                    labFee = r["LabFee"],
                                    totalFee = r["TotalFee"],
                                    discountAmount = r["DiscountAmount"],
                                    netAssessment = r["NetAssessment"],
                                    percentRequired = r["PercentRequired"],
                                    amountDue = r["AmountDue"],
                                    totalPaid = r["TotalPaid"],
                                    status = r["Status"],
                                    assessedAt = ((DateTime)r["AssessedAt"]).ToString("MMM dd, yyyy"),
                                    overpaidAmount = r["OverpaidAmount"]
                                });
                    }

                    // 3. All payments
                    var payments = new List<object>();
                    using (var cmd = new SqlCommand(@"
                        SELECT p.PaymentID, p.AssessmentID, p.ORNumber,
                               p.AmountPaid, p.PaymentMode, p.Period,
                               p.SchoolYear, p.Semester, p.YearLevel,
                               p.PaymentDate, p.Remarks, p.TransactionType
                        FROM Payments p
                        WHERE p.StudentID = @sid
                        ORDER BY p.PaymentDate ASC", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                payments.Add(new
                                {
                                    paymentID = r["PaymentID"],
                                    assessmentID = r["AssessmentID"],
                                    orNumber = r["ORNumber"],
                                    amountPaid = r["AmountPaid"],
                                    paymentMode = r["PaymentMode"],
                                    period = r["Period"],
                                    schoolYear = r["SchoolYear"],
                                    semester = r["Semester"],
                                    yearLevel = r["YearLevel"],
                                    paymentDate = ((DateTime)r["PaymentDate"]).ToString("MMM dd, yyyy"),
                                    remarks = r["Remarks"] == DBNull.Value ? "" : r["Remarks"],
                                    transactionType = r["TransactionType"]
                                });
                    }

                    // 4. Refund requests
                    var refunds = new List<object>();
                    using (var cmd = new SqlCommand(@"
    SELECT p.PaymentID, p.AmountPaid, p.PaymentDate, 
           p.Semester, p.SchoolYear, p.YearLevel, p.Remarks
    FROM Payments p
    WHERE p.StudentID = @sid
      AND p.TransactionType = 'Refund'
    ORDER BY p.PaymentDate ASC", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                refunds.Add(new
                                {
                                    requestID = r["PaymentID"],
                                    amount = r["AmountPaid"],
                                    requestDate = ((DateTime)r["PaymentDate"]).ToString("MMM dd, yyyy"),
                                    status = "Approved",
                                    semester = r["Semester"],
                                    schoolYear = r["SchoolYear"],
                                    yearLevel = r["YearLevel"],
                                    remarks = r["Remarks"] == DBNull.Value ? "" : r["Remarks"]
                                });
                    }

                    data.Add(new
                    {
                        mess = 1,
                        studentInfo = studentInfo,
                        assessments = assessments,
                        payments = payments,
                        refunds = refunds
                    });
                }
            }
            catch (Exception ex)
            {
                data.Add(new { mess = 0, message = ex.Message });
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // ── ASSESSMENT REPORT ─────────────────────────────────────────
        public ActionResult AssessmentReport()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            var courses = new List<SelectListItem>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT CourseID, CourseCode FROM Courses WHERE IsActive=1", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        courses.Add(new SelectListItem
                        {
                            Value = r["CourseID"].ToString(),
                            Text = r["CourseCode"].ToString()
                        });
            }
            ViewBag.Courses = courses;
            return View();
        }

        public ActionResult GetAssessmentReport(string schoolYear, string semester,
            string courseID, string yearLevel, string period, string status)
        {
            var data = new List<object>();
            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();

                    string sql = @"
                        SELECT s.StudentID, s.LastName + ', ' + s.FirstName + ' ' + ISNULL(s.MiddleName, '') AS StudentName,
                               c.CourseCode, c.CourseName,
                               a.YearLevel, a.SchoolYear, a.Semester, a.Period,
                               a.TotalFee, a.TuitionFee, a.RegistrationFee,
                               a.MiscFee, a.LabFee, a.DiscountAmount,
                               a.NetAssessment, a.AmountDue, a.Status,
                               ISNULL(SUM(p.AmountPaid), 0) AS TotalPaid,
                               a.AmountDue - ISNULL(SUM(p.AmountPaid), 0) AS Balance
                        FROM Assessments a
                        JOIN Students s ON s.StudentID = a.StudentID
                        JOIN Courses c  ON c.CourseID  = s.CourseID
                        LEFT JOIN Payments p ON p.AssessmentID = a.AssessmentID
                                            AND p.TransactionType = 'Payment'
                        WHERE (@sy IS NULL OR a.SchoolYear = @sy)
                            AND (@sem IS NULL OR a.Semester = @sem)";

                    if (!string.IsNullOrEmpty(courseID))
                        sql += " AND s.CourseID = @cid";
                    if (!string.IsNullOrEmpty(yearLevel))
                        sql += " AND a.YearLevel = @yl";
                    if (!string.IsNullOrEmpty(period))
                        sql += " AND a.Period = @period";
                    if (!string.IsNullOrEmpty(status))
                        sql += " AND a.Status = @status";

                    sql += @" GROUP BY s.StudentID, s.FirstName, s.LastName, s.MiddleName,
                               c.CourseCode, c.CourseName, a.YearLevel,
                               a.SchoolYear, a.Semester, a.Period,
                               a.TotalFee, a.TuitionFee, a.RegistrationFee,
                               a.MiscFee, a.LabFee, a.DiscountAmount,
                               a.NetAssessment, a.AmountDue, a.Status
                             ORDER BY s.LastName, a.YearLevel, a.Semester,
                               CASE a.Period
                                   WHEN 'Prelim'     THEN 1
                                   WHEN 'Midterm'    THEN 2
                                   WHEN 'Semi-Final' THEN 3
                                   WHEN 'Final'      THEN 4
                               END";

                    using (var cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.AddWithValue("@sy", string.IsNullOrEmpty(schoolYear) ? (object)DBNull.Value : schoolYear);
                        cmd.Parameters.AddWithValue("@sem", string.IsNullOrEmpty(semester) ? (object)DBNull.Value : semester);
                        if (!string.IsNullOrEmpty(courseID))
                            cmd.Parameters.AddWithValue("@cid", int.Parse(courseID));
                        if (!string.IsNullOrEmpty(yearLevel))
                            cmd.Parameters.AddWithValue("@yl", int.Parse(yearLevel));
                        if (!string.IsNullOrEmpty(period))
                            cmd.Parameters.AddWithValue("@period", period);
                        if (!string.IsNullOrEmpty(status))
                            cmd.Parameters.AddWithValue("@status", status);

                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                data.Add(new
                                {
                                    studentID = r["StudentID"],
                                    studentName = r["StudentName"],
                                    courseCode = r["CourseCode"],
                                    courseName = r["CourseName"],
                                    yearLevel = r["YearLevel"],
                                    schoolYear = r["SchoolYear"],
                                    semester = r["Semester"],
                                    period = r["Period"],
                                    totalFee = r["TotalFee"],
                                    tuitionFee = r["TuitionFee"],
                                    registrationFee = r["RegistrationFee"],
                                    miscFee = r["MiscFee"],
                                    labFee = r["LabFee"],
                                    discountAmount = r["DiscountAmount"],
                                    netAssessment = r["NetAssessment"],
                                    amountDue = r["AmountDue"],
                                    totalPaid = r["TotalPaid"],
                                    balance = r["Balance"],
                                    status = r["Status"]
                                });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new List<object> { new { mess = 0, message = ex.Message } },
                    JsonRequestBehavior.AllowGet);
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }
    }
}