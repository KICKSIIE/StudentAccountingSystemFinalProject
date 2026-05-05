using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using StudentAccountingSystem.Models;

namespace StudentAccountingSystem.Controllers
{
    public class StudentController : Controller
    {
        private readonly string _conn = ConfigurationManager
            .ConnectionStrings["StudentAccountingDB"].ConnectionString;

        public ActionResult Index()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");
            return View();
        }

        public ActionResult GetAllStudents()
        {
            try
            {
                var data = new List<object>();
                using (var con = new SqlConnection(_conn))
                using (var cmd = new SqlCommand(
                    @"SELECT s.StudentID, s.LastName, s.FirstName,
                     s.YearLevel, s.Semester, s.SchoolYear, s.Status,
                     CASE 
                         WHEN c.CourseCode IS NOT NULL THEN c.CourseCode
                         ELSE 'Unknown Course (ID: ' + CAST(s.CourseID AS VARCHAR) + ')'
                     END as CourseCode
              FROM   Students s
              LEFT JOIN Courses c ON s.CourseID = c.CourseID
              ORDER  BY s.LastName, s.FirstName", con))
                {
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            data.Add(new
                            {
                                tid = r["StudentID"],
                                idno = r["StudentID"],  // StudentID is now the student number
                                lastname = r["LastName"],
                                firstname = r["FirstName"],
                                course = r["CourseCode"],
                                year = r["YearLevel"],
                                semester = r["Semester"],
                                schoolYear = r["SchoolYear"],
                                status = r["Status"]
                            });
                }
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in GetAllStudents: " + ex.Message);
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetCourseList()
        {
            var data = new List<object>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT CourseID, CourseCode, CourseName FROM Courses WHERE IsActive=1", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        data.Add(new
                        {
                            courseID = r["CourseID"],
                            courseCode = r["CourseCode"],
                            courseName = r["CourseName"]
                        });
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetScholarshipList()
        {
            var data = new List<object>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT ScholarshipID, ScholarshipName, Type, DiscountPercent, DiscountAmount FROM Scholarships WHERE IsActive=1", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        data.Add(new
                        {
                            scholarshipID = r["ScholarshipID"],
                            scholarshipName = r["ScholarshipName"],
                            type = r["Type"],
                            discountPercent = r["DiscountPercent"],
                            discountAmount = r["DiscountAmount"]
                        });
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetDiscountList()
        {
            var data = new List<object>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT DiscountID, DiscountName, Type, DiscountPercent, DiscountAmount FROM Discounts WHERE IsActive=1", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        data.Add(new
                        {
                            discountID = r["DiscountID"],
                            discountName = r["DiscountName"],
                            type = r["Type"],
                            discountPercent = r["DiscountPercent"],
                            discountAmount = r["DiscountAmount"]
                        });
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // 1. REPLACE GenerateNumericStudentNumber() — query StudentID now, not StudentNumber
        private int GenerateStudentID()
        {
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                using (var cmd = new SqlCommand(
                    "SELECT ISNULL(MAX(StudentID), 999999999) FROM Students", con))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar()) + 1;
                }
            }
        }

        // 2. REPLACE RegisterStudent() — no more StudentNumber column, StudentID is inserted manually
        public ActionResult RegisterStudent()
        {
            var data = new List<object>();

            try
            {
                string lastName = Request["lastName"];
                string firstName = Request["firstName"];
                string middleName = Request["middleName"];
                string gender = Request["gender"];
                string birthDate = Request["birthDate"];
                string contact = Request["contact"];
                string address = Request["address"];
                string email = Request["email"];
                string courseID = Request["courseID"];
                string yearLevel = Request["yearLevel"];
                string semester = Request["semester"];
                string schoolYear = Request["schoolYear"];
                string scholarshipID = Request["scholarshipID"];
                string discountID = Request["discountID"];

                if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName)
                    || string.IsNullOrWhiteSpace(courseID) || string.IsNullOrWhiteSpace(yearLevel)
                    || string.IsNullOrWhiteSpace(semester) || string.IsNullOrWhiteSpace(schoolYear))
                {
                    data.Add(new { mess = 0, message = "Please fill in all required fields." });
                    return Json(data, JsonRequestBehavior.AllowGet);
                }

                int newStudentID = GenerateStudentID(); // StudentID IS the student number now

                using (var con = new SqlConnection(_conn))
                {
                    con.Open();

                    // INSERT — StudentID is explicitly provided, no SCOPE_IDENTITY needed
                    using (var cmd = new SqlCommand(
                        @"INSERT INTO Students
                    (StudentID, LastName, FirstName, MiddleName, Gender,
                     BirthDate, ContactNumber, Address, Email, CourseID,
                     YearLevel, Semester, SchoolYear, Status, EnrolledAt,
                     ScholarshipID, DiscountID)
                  VALUES
                    (@sid, @lname, @fname, @mname, @gender,
                     @bdate, @contact, @address, @email, @courseID,
                     @year, @sem, @sy, 'Enrolled', GETDATE(),
                     @schid, @did)", con))
                    {
                        cmd.Parameters.AddWithValue("@sid", newStudentID);
                        cmd.Parameters.AddWithValue("@lname", lastName.Trim().ToUpper());
                        cmd.Parameters.AddWithValue("@fname", firstName.Trim().ToUpper());
                        cmd.Parameters.AddWithValue("@mname", string.IsNullOrEmpty(middleName) ? (object)DBNull.Value : middleName.Trim().ToUpper());
                        cmd.Parameters.AddWithValue("@gender", gender);
                        cmd.Parameters.AddWithValue("@bdate", DateTime.ParseExact(birthDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                        cmd.Parameters.AddWithValue("@contact", string.IsNullOrEmpty(contact) ? (object)DBNull.Value : contact.Trim());
                        cmd.Parameters.AddWithValue("@address", string.IsNullOrEmpty(address) ? (object)DBNull.Value : address.Trim());
                        cmd.Parameters.AddWithValue("@email", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email.Trim());
                        cmd.Parameters.AddWithValue("@courseID", int.Parse(courseID));
                        cmd.Parameters.AddWithValue("@year", int.Parse(yearLevel));
                        cmd.Parameters.AddWithValue("@sem", semester);
                        cmd.Parameters.AddWithValue("@sy", schoolYear.Trim());
                        cmd.Parameters.AddWithValue("@schid", string.IsNullOrEmpty(scholarshipID) ? (object)DBNull.Value : int.Parse(scholarshipID));
                        cmd.Parameters.AddWithValue("@did", string.IsNullOrEmpty(discountID) ? (object)DBNull.Value : int.Parse(discountID));
                        cmd.ExecuteNonQuery();
                    }

                    // GenerateAssessments — pass newStudentID as long
                    GenerateAssessments(con, newStudentID, int.Parse(courseID),
                        int.Parse(yearLevel), semester, schoolYear.Trim());

                    
                }

                data.Add(new { mess = 1, studentNumber = newStudentID.ToString(), message = "Student registered successfully." });
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: " + ex.Message);
                data.Add(new { mess = 0, message = "Error: " + ex.Message });
                return Json(data, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetActiveStudents()
        {
            var data = new List<object>();
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
            SELECT  s.StudentID,
                    s.FirstName + ' ' + s.LastName AS StudentName,
                    c.CourseCode,
                    s.YearLevel,
                    s.Semester,
                    s.SchoolYear,
                    s.Status
            FROM    Students s
            JOIN    Courses c ON c.CourseID = s.CourseID
            WHERE   s.Status IN ('Enrolled', 'Active')  -- EXCLUDES 'Graduated'
            ORDER BY s.LastName, s.FirstName", con))
                {
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            data.Add(new
                            {
                                StudentID = r["StudentID"],
                                StudentName = r["StudentName"],
                                CourseCode = r["CourseCode"],
                                YearLevel = r["YearLevel"],
                                Semester = r["Semester"],
                                SchoolYear = r["SchoolYear"],
                                Status = r["Status"]
                            });
                        }
                    }
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetStudent()
        {
            string id = Request["studentID"];
            var data = new List<object>();

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT s.StudentID, s.LastName, s.FirstName,
             s.MiddleName, s.Gender, s.BirthDate, s.ContactNumber,
             s.Address, s.Email, s.CourseID, s.YearLevel,
             s.Semester, s.SchoolYear, s.Status,
             s.ScholarshipID, s.DiscountID
          FROM   Students s
          WHERE  s.StudentID = @id
            AND  s.Status IN ('Enrolled', 'Active')", con))  // ADD THIS LINE to exclude graduated
            {
                cmd.Parameters.AddWithValue("@id", int.Parse(id));
                con.Open();
                using (var r = cmd.ExecuteReader())
                    if (r.Read())
                        data.Add(new
                        {
                            studentID = r["StudentID"],
                            lastName = r["LastName"],
                            firstName = r["FirstName"],
                            middleName = r["MiddleName"] == DBNull.Value ? "" : r["MiddleName"],
                            gender = r["Gender"],
                            birthDate = ((DateTime)r["BirthDate"]).ToString("yyyy-MM-dd"),
                            contact = r["ContactNumber"] == DBNull.Value ? "" : r["ContactNumber"],
                            address = r["Address"] == DBNull.Value ? "" : r["Address"],
                            email = r["Email"] == DBNull.Value ? "" : r["Email"],
                            courseID = r["CourseID"],
                            yearLevel = r["YearLevel"],
                            semester = r["Semester"],
                            schoolYear = r["SchoolYear"],
                            status = r["Status"],
                            scholarshipID = r["ScholarshipID"] == DBNull.Value ? "" : r["ScholarshipID"].ToString(),
                            discountID = r["DiscountID"] == DBNull.Value ? "" : r["DiscountID"].ToString()
                        });
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Details(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            Student student = null;
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT s.*, 
                 c.CourseCode + ' - ' + c.CourseName AS CourseDisplay,
                 sc.ScholarshipName, 
                 d.DiscountName
          FROM Students s
          LEFT JOIN Courses      c  ON s.CourseID      = c.CourseID
          LEFT JOIN Scholarships sc ON s.ScholarshipID = sc.ScholarshipID
          LEFT JOIN Discounts    d  ON s.DiscountID    = d.DiscountID
          WHERE s.StudentID = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                con.Open();
                using (var r = cmd.ExecuteReader())
                    if (r.Read())
                    {
                        student = new Student
                        {
                            StudentID = (int)r["StudentID"],
                            LastName = r["LastName"].ToString(),
                            FirstName = r["FirstName"].ToString(),
                            MiddleName = r["MiddleName"] == DBNull.Value ? null : r["MiddleName"].ToString(),
                            Gender = r["Gender"].ToString(),
                            BirthDate = (DateTime)r["BirthDate"],
                            ContactNumber = r["ContactNumber"] == DBNull.Value ? null : r["ContactNumber"].ToString(),
                            Address = r["Address"] == DBNull.Value ? null : r["Address"].ToString(),
                            Email = r["Email"] == DBNull.Value ? null : r["Email"].ToString(),
                            CourseID = (int)r["CourseID"],
                            YearLevel = (int)r["YearLevel"],
                            Semester = r["Semester"].ToString(),
                            SchoolYear = r["SchoolYear"].ToString(),
                            Status = r["Status"].ToString(),
                            ScholarshipID = r["ScholarshipID"] == DBNull.Value ? (int?)null : (int)r["ScholarshipID"],
                            DiscountID = r["DiscountID"] == DBNull.Value ? (int?)null : (int)r["DiscountID"]
                        };

                        // Store the display names in ViewBag
                        ViewBag.CourseDisplay = r["CourseDisplay"].ToString();
                        ViewBag.ScholarshipDisplay = r["ScholarshipName"] == DBNull.Value ? "None" : r["ScholarshipName"].ToString();
                        ViewBag.DiscountDisplay = r["DiscountName"] == DBNull.Value ? "None" : r["DiscountName"].ToString();
                    }
            }

            if (student == null) return HttpNotFound();
            return View(student);
        }

        public ActionResult UpdateStudent()
        {
            string studentID = Request["studentID"];
            string lastName = Request["lastName"];
            string firstName = Request["firstName"];
            string middleName = Request["middleName"];
            string gender = Request["gender"];
            string birthDate = Request["birthDate"];
            string contact = Request["contact"];
            string address = Request["address"];
            string email = Request["email"];
            string courseID = Request["courseID"];
            string yearLevel = Request["yearLevel"];
            string semester = Request["semester"];
            string schoolYear = Request["schoolYear"];
            string status = Request["status"];
            string scholarshipID = Request["scholarshipID"];
            string discountID = Request["discountID"];

            var data = new List<object>();

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"UPDATE Students SET
            LastName=@lname, FirstName=@fname, MiddleName=@mname,
            Gender=@gender, BirthDate=@bdate, ContactNumber=@contact,
            Address=@address, Email=@email, CourseID=@courseID,
            YearLevel=@year, Semester=@sem, SchoolYear=@sy,
            Status=@status, ScholarshipID=@schid, DiscountID=@did
          WHERE StudentID=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", int.Parse(studentID)); 
                cmd.Parameters.AddWithValue("@lname", lastName.Trim().ToUpper());
                cmd.Parameters.AddWithValue("@fname", firstName.Trim().ToUpper());
                cmd.Parameters.AddWithValue("@mname", string.IsNullOrEmpty(middleName) ? (object)DBNull.Value : middleName.Trim().ToUpper());
                cmd.Parameters.AddWithValue("@gender", gender);
                cmd.Parameters.AddWithValue("@bdate", DateTime.ParseExact(birthDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                cmd.Parameters.AddWithValue("@contact", string.IsNullOrEmpty(contact) ? (object)DBNull.Value : contact);
                cmd.Parameters.AddWithValue("@address", string.IsNullOrEmpty(address) ? (object)DBNull.Value : address);
                cmd.Parameters.AddWithValue("@email", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email);
                cmd.Parameters.AddWithValue("@courseID", int.Parse(courseID));
                cmd.Parameters.AddWithValue("@year", int.Parse(yearLevel));
                cmd.Parameters.AddWithValue("@sem", semester);
                cmd.Parameters.AddWithValue("@sy", schoolYear);
                cmd.Parameters.AddWithValue("@status", status);
                cmd.Parameters.AddWithValue("@schid", string.IsNullOrEmpty(scholarshipID) ? (object)DBNull.Value : int.Parse(scholarshipID));
                cmd.Parameters.AddWithValue("@did", string.IsNullOrEmpty(discountID) ? (object)DBNull.Value : int.Parse(discountID));
                con.Open();
                cmd.ExecuteNonQuery();
            }

            data.Add(new { mess = 1, message = "Student updated successfully." });
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DeleteStudent()
        {
            string id = Request["studentID"];
            var data = new List<object>();

            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();
                    foreach (var sql in new[] {
                "DELETE FROM Payments    WHERE StudentID=@id",
                "DELETE FROM Assessments WHERE StudentID=@id",
                "DELETE FROM Users       WHERE StudentID=@id",
                "DELETE FROM Students    WHERE StudentID=@id"
            })
                    {
                        using (var cmd = new SqlCommand(sql, con))
                        {
                            cmd.Parameters.AddWithValue("@id", int.Parse(id)); 
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                data.Add(new { mess = 1, message = "Student deleted." });
            }
            catch
            {
                data.Add(new { mess = 0, message = "Could not delete student." });
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        // keep GenerateAssessments and AdvanceSemester exactly as they are
        // (they are used by StudentPortalController — do not remove)
        public static void GenerateAssessments(SqlConnection con, int studentID, int courseID,
int yearLevel, string semester, string schoolYear, SqlTransaction trans = null)
        {
            decimal tuitionPerUnit = 0, regFee = 0, miscFee = 0, labFee = 0;
            int units = 0;

            // IMPORTANT: Every SqlCommand MUST now include 'trans'
            using (var cmd = new SqlCommand(@"SELECT Units, TuitionPerUnit, RegistrationFee, MiscFee, LabFee
                              FROM Courses WHERE CourseID = @cid", con, trans))
            {
                cmd.Parameters.AddWithValue("@cid", courseID);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return;
                    units = (int)r["Units"];
                    tuitionPerUnit = (decimal)r["TuitionPerUnit"];
                    regFee = (decimal)r["RegistrationFee"];
                    miscFee = (decimal)r["MiscFee"];
                    labFee = (decimal)r["LabFee"];
                }
            }

            decimal tuitionFee = tuitionPerUnit * units;
            decimal totalFee = tuitionFee + regFee + miscFee + labFee;
            decimal discountAmount = 0;

            using (var cmd = new SqlCommand(@"SELECT sc.Type AS SchType, sc.DiscountPercent AS SchPct, sc.DiscountAmount AS SchAmt,
                                 d.Type  AS DisType, d.DiscountPercent  AS DisPct, d.DiscountAmount  AS DisAmt
                          FROM Students s
                          LEFT JOIN Scholarships sc ON sc.ScholarshipID = s.ScholarshipID
                          LEFT JOIN Discounts     d ON d.DiscountID     = s.DiscountID
                          WHERE s.StudentID = @sid", con, trans))
            {
                cmd.Parameters.AddWithValue("@sid", studentID);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        // DEBUG
                        System.Diagnostics.Debug.WriteLine($"SchType: {r["SchType"]}");
                        System.Diagnostics.Debug.WriteLine($"SchPct:  {r["SchPct"]}");
                        System.Diagnostics.Debug.WriteLine($"SchAmt:  {r["SchAmt"]}");
                        System.Diagnostics.Debug.WriteLine($"DisType: {r["DisType"]}");
                        System.Diagnostics.Debug.WriteLine($"DisPct:  {r["DisPct"]}");
                        System.Diagnostics.Debug.WriteLine($"DisAmt:  {r["DisAmt"]}");

                        // SCHOLARSHIP
                        if (r["SchType"] != DBNull.Value)
                        {
                            if (r["SchType"].ToString() == "Percent" && r["SchPct"] != DBNull.Value)
                                discountAmount += totalFee * Convert.ToDecimal(r["SchPct"]) / 100;
                            else if (r["SchType"].ToString() == "Fixed" && r["SchAmt"] != DBNull.Value)
                                discountAmount += Convert.ToDecimal(r["SchAmt"]);
                        }

                        // DISCOUNT
                        if (r["DisType"] != DBNull.Value)
                        {
                            if (r["DisType"].ToString() == "Percent" && r["DisPct"] != DBNull.Value)
                                discountAmount += totalFee * Convert.ToDecimal(r["DisPct"]) / 100;
                            else if (r["DisType"].ToString() == "Fixed" && r["DisAmt"] != DBNull.Value)
                                discountAmount += Convert.ToDecimal(r["DisAmt"]);
                        }

                        System.Diagnostics.Debug.WriteLine($"discountAmount: {discountAmount}, totalFee: {totalFee}");
                    }
                }
            }

            if (discountAmount > totalFee) discountAmount = totalFee;
            decimal netAssessment = Math.Max(0, totalFee - discountAmount);

            // Calculate individual period amounts
            decimal prelimAmount = Math.Round(netAssessment * 53 / 100, 2);
            decimal midtermAmount = Math.Round(netAssessment * 11 / 100, 2);
            decimal semiFinalAmount = Math.Round(netAssessment * 11 / 100, 2);
            decimal finalAmount = Math.Round(netAssessment * 25 / 100, 2);

            // Determine statuses based on whether amounts are zero
            string prelimStatus = "Unpaid";
            string midtermStatus = "Locked";
            string semiFinalStatus = "Locked";
            string finalStatus = "Locked";

            // If netAssessment is ZERO, handle accordingly
            if (netAssessment == 0)
            {
                // All periods have zero balance - mark them all as Paid
                prelimStatus = "Paid";
                midtermStatus = "Paid";
                semiFinalStatus = "Paid";
                finalStatus = "Paid";
            }
            else
            {
                // Check each period individually for zero balance
                if (prelimAmount == 0)
                {
                    prelimStatus = "Paid";
                    midtermStatus = "Unpaid";  // Unlock next period
                }

                if (midtermAmount == 0 && midtermStatus == "Unpaid")
                {
                    midtermStatus = "Paid";
                    semiFinalStatus = "Unpaid";
                }

                if (semiFinalAmount == 0 && semiFinalStatus == "Unpaid")
                {
                    semiFinalStatus = "Paid";
                    finalStatus = "Unpaid";
                }

                if (finalAmount == 0 && finalStatus == "Unpaid")
                {
                    finalStatus = "Paid";
                }
            }

            // Array with calculated statuses
            var periods = new[]
            {
        new { Period = "Prelim",     Amount = prelimAmount,     Status = prelimStatus },
        new { Period = "Midterm",    Amount = midtermAmount,    Status = midtermStatus },
        new { Period = "Semi-Final", Amount = semiFinalAmount,  Status = semiFinalStatus },
        new { Period = "Final",      Amount = finalAmount,      Status = finalStatus }
    };

            foreach (var p in periods)
            {
                try
                {
                    int exists = 0;
                    using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM Assessments
            WHERE StudentID=@sid AND SchoolYear=@sy AND Semester=@sem 
            AND Period=@period AND Status != 'Refunded'", con, trans))
                    {
                        cmd.Parameters.AddWithValue("@sid", studentID);
                        cmd.Parameters.AddWithValue("@sy", schoolYear);
                        cmd.Parameters.AddWithValue("@sem", semester);
                        cmd.Parameters.AddWithValue("@period", p.Period);
                        exists = (int)cmd.ExecuteScalar();
                    }

                    if (exists > 0)
                    {
                        // Log that we skipped this period
                        System.Diagnostics.Debug.WriteLine($"Skipping {p.Period} - already exists");
                        continue;
                    }

                    using (var cmd = new SqlCommand(@"INSERT INTO Assessments 
        (StudentID, YearLevel, CourseFeeID, SchoolYear, Semester, Period, 
         TotalFee, TuitionFee, RegistrationFee, MiscFee, LabFee, 
         DiscountAmount, NetAssessment, PercentRequired, 
         AmountDue, Status, AssessedAt) 
        VALUES 
        (@sid, @yl, NULL, @sy, @sem, @period, 
         @totalFee, @tuitionFee, @regFee, @miscFee, @labFee, 
         @discount, @net, @pct, 
         @amountDue, @status, GETDATE());
        SELECT SCOPE_IDENTITY();", con, trans))  // <-- added here
                    {
                        decimal pct = 0;
                        if (p.Period == "Prelim") pct = 53;
                        else if (p.Period == "Midterm") pct = 11;
                        else if (p.Period == "Semi-Final") pct = 11;
                        else if (p.Period == "Final") pct = 25;

                        cmd.Parameters.AddWithValue("@sid", studentID);
                        cmd.Parameters.AddWithValue("@yl", yearLevel);
                        cmd.Parameters.AddWithValue("@sy", schoolYear);
                        cmd.Parameters.AddWithValue("@sem", semester);
                        cmd.Parameters.AddWithValue("@period", p.Period);
                        cmd.Parameters.AddWithValue("@totalFee", totalFee);
                        cmd.Parameters.AddWithValue("@tuitionFee", tuitionFee);
                        cmd.Parameters.AddWithValue("@regFee", regFee);
                        cmd.Parameters.AddWithValue("@miscFee", miscFee);
                        cmd.Parameters.AddWithValue("@labFee", labFee);
                        cmd.Parameters.AddWithValue("@discount", discountAmount);
                        cmd.Parameters.AddWithValue("@net", netAssessment);
                        cmd.Parameters.AddWithValue("@pct", pct);
                        cmd.Parameters.AddWithValue("@amountDue", p.Amount);
                        cmd.Parameters.AddWithValue("@status", p.Status);

                        // ExecuteScalar returns the new AssessmentID in one round-trip
                        int newAssessmentID = Convert.ToInt32(cmd.ExecuteScalar());

                        if (p.Status == "Paid")
                        {
                            using (var payCmd = new SqlCommand(@"
                                INSERT INTO Payments 
                                    (AssessmentID, StudentID, AmountPaid, PaymentDate, 
                                     PaymentMode, Remarks, TransactionType,
                                     Period, SchoolYear, Semester, YearLevel)
                                VALUES 
                                    (@aid, @sid, 0, GETDATE(), 
                                     'Discount', '100% discount/scholarship - no payment collected', 'Discount Adjustment',
                                     @period, @sy, @sem, @yl)", con, trans))
                            {
                                payCmd.Parameters.AddWithValue("@aid", newAssessmentID);
                                payCmd.Parameters.AddWithValue("@sid", studentID);
                                payCmd.Parameters.AddWithValue("@period", p.Period);
                                payCmd.Parameters.AddWithValue("@sy", schoolYear);
                                payCmd.Parameters.AddWithValue("@sem", semester);
                                payCmd.Parameters.AddWithValue("@yl", yearLevel);
                                payCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error inserting {p.Period}: {ex.Message}");
                    throw; // Re-throw to see what's happening
                }
            }
        }

        public static void AdvanceSemester(SqlConnection con, int studentID)
        {
            int yearLevel = 0; string semester = "", schoolYear = ""; int courseID = 0;
            using (var cmd = new SqlCommand(
                "SELECT YearLevel, Semester, SchoolYear, CourseID FROM Students WHERE StudentID=@sid", con))
            {
                cmd.Parameters.AddWithValue("@sid", studentID);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return;
                    yearLevel = (int)r["YearLevel"];
                    semester = r["Semester"].ToString();
                    schoolYear = r["SchoolYear"].ToString();
                    courseID = (int)r["CourseID"];
                }
            }

            // --- UPDATED GRADUATION BLOCK ---
            if (yearLevel == 4 && semester == "2nd")
            {
                // 1. Get the excess from the last row before we "close" the student account
                decimal graduationBalance = 0;
                using (var cmd = new SqlCommand("SELECT ISNULL(OverpaidAmount, 0) FROM Assessments WHERE StudentID=@sid AND Period='Final' AND YearLevel=4", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    graduationBalance = (decimal)cmd.ExecuteScalar();
                }

                // 2. Set status to Graduated
                using (var cmd = new SqlCommand("UPDATE Students SET Status='Graduated' WHERE StudentID=@sid", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    cmd.ExecuteNonQuery();
                }

                // Note: The money stays in the Year 4 Final Assessment row, 
                // which makes it visible for the Refund Request button.
                return;
            }

            int nextYear = yearLevel; string nextSem = semester, nextSY = schoolYear;
            if (semester == "1st") { nextSem = "2nd"; }
            else
            {
                nextSem = "1st"; nextYear = yearLevel + 1;
                var pts = schoolYear.Split('-');
                if (pts.Length == 2 && int.TryParse(pts[0], out int s1) && int.TryParse(pts[1], out int s2))
                    nextSY = (s1 + 1) + "-" + (s2 + 1);
            }

            using (var cmd = new SqlCommand(
                "UPDATE Students SET YearLevel=@y, Semester=@s, SchoolYear=@sy WHERE StudentID=@sid", con))
            {
                cmd.Parameters.AddWithValue("@y", nextYear);
                cmd.Parameters.AddWithValue("@s", nextSem);
                cmd.Parameters.AddWithValue("@sy", nextSY);
                cmd.Parameters.AddWithValue("@sid", studentID);
                cmd.ExecuteNonQuery();
            }

            // THIS IS CORRECT: You are passing 'nextYear' into the assessment generator.
            // Ensure GenerateAssessments is updated to handle this new parameter (as we did in the previous step).
            GenerateAssessments(con, studentID, courseID, nextYear, nextSem, nextSY);
        }

        public ActionResult Edit(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            Student student = null;
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                using (var cmd = new SqlCommand(
                    @"SELECT * FROM Students WHERE StudentID = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read())
                            student = new Student
                            {
                                StudentID = (int)r["StudentID"],
                                LastName = r["LastName"].ToString(),
                                FirstName = r["FirstName"].ToString(),
                                MiddleName = r["MiddleName"] == DBNull.Value ? null : r["MiddleName"].ToString(),
                                Gender = r["Gender"].ToString(),
                                BirthDate = (DateTime)r["BirthDate"],
                                ContactNumber = r["ContactNumber"] == DBNull.Value ? null : r["ContactNumber"].ToString(),
                                Address = r["Address"] == DBNull.Value ? null : r["Address"].ToString(),
                                Email = r["Email"] == DBNull.Value ? null : r["Email"].ToString(),
                                CourseID = (int)r["CourseID"],
                                YearLevel = (int)r["YearLevel"],
                                Semester = r["Semester"].ToString(),
                                SchoolYear = r["SchoolYear"].ToString(),
                                Status = r["Status"].ToString(),
                                ScholarshipID = r["ScholarshipID"] == DBNull.Value ? (int?)null : (int)r["ScholarshipID"],
                                DiscountID = r["DiscountID"] == DBNull.Value ? (int?)null : (int)r["DiscountID"]
                            };
                }
            }

            if (student == null) return HttpNotFound();

            // Populate courses dropdown
            var courses = new List<SelectListItem>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT CourseID, CourseCode + ' - ' + CourseName AS Label FROM Courses WHERE IsActive=1", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        courses.Add(new SelectListItem
                        {
                            Value = r["CourseID"].ToString(),
                            Text = r["Label"].ToString()
                        });
            }
            ViewBag.Courses = courses;

            // Populate school years dropdown
            var schoolYears = new List<SelectListItem>();
            int currYear = DateTime.Now.Year;
            for (int y = currYear - 2; y <= currYear + 3; y++)
            {
                string sy = y + "-" + (y + 1);
                schoolYears.Add(new SelectListItem { Value = sy, Text = sy });
            }
            ViewBag.SchoolYears = schoolYears;

            // Populate scholarships dropdown
            var scholarships = new List<SelectListItem>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT ScholarshipID, ScholarshipName FROM Scholarships WHERE IsActive=1", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        scholarships.Add(new SelectListItem
                        {
                            Value = r["ScholarshipID"].ToString(),
                            Text = r["ScholarshipName"].ToString()
                        });
            }
            ViewBag.Scholarships = scholarships;

            // Populate discounts dropdown
            var discounts = new List<SelectListItem>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT DiscountID, DiscountName FROM Discounts WHERE IsActive=1", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        discounts.Add(new SelectListItem
                        {
                            Value = r["DiscountID"].ToString(),
                            Text = r["DiscountName"].ToString()
                        });
            }
            ViewBag.Discounts = discounts;

            return View(student);
        }

        public ActionResult Create()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            // Courses dropdown
            var courses = new List<SelectListItem>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT CourseID, CourseCode, CourseName FROM Courses WHERE IsActive=1", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        courses.Add(new SelectListItem
                        {
                            Value = r["CourseID"].ToString(),
                            Text = r["CourseCode"].ToString() + " - " + r["CourseName"].ToString()
                        });
            }
            ViewBag.Courses = courses;

            // Scholarships dropdown
            var scholarships = new List<SelectListItem>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT ScholarshipID, ScholarshipName, Type, DiscountPercent, DiscountAmount FROM Scholarships WHERE IsActive=1", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                    {
                        var val = r["Type"].ToString() == "Percent"
                            ? r["DiscountPercent"].ToString() + "%"
                            : "₱" + r["DiscountAmount"].ToString();
                        scholarships.Add(new SelectListItem
                        {
                            Value = r["ScholarshipID"].ToString(),
                            Text = r["ScholarshipName"].ToString() + " (" + val + ")"
                        });
                    }
            }
            ViewBag.Scholarships = scholarships;

            // Discounts dropdown
            var discounts = new List<SelectListItem>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT DiscountID, DiscountName, Type, DiscountPercent, DiscountAmount FROM Discounts WHERE IsActive=1", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                    {
                        var val = r["Type"].ToString() == "Percent"
                            ? r["DiscountPercent"].ToString() + "%"
                            : "₱" + r["DiscountAmount"].ToString();
                        discounts.Add(new SelectListItem
                        {
                            Value = r["DiscountID"].ToString(),
                            Text = r["DiscountName"].ToString() + " (" + val + ")"
                        });
                    }
            }
            ViewBag.Discounts = discounts;

            return View();
        }

        public ActionResult GetStudentStatus(int studentID)
        {
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                using (var cmd = new SqlCommand("SELECT Status FROM Students WHERE StudentID = @sid", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    var status = cmd.ExecuteScalar()?.ToString();
                    return Json(new { status = status ?? "Unknown" }, JsonRequestBehavior.AllowGet);
                }
            }
        }


    }
}