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
                    @"SELECT s.StudentID, s.StudentNumber, s.LastName, s.FirstName,
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
                                idno = r["StudentNumber"],
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

                // Validate required fields
                if (string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(firstName)
                    || string.IsNullOrWhiteSpace(courseID) || string.IsNullOrWhiteSpace(yearLevel)
                    || string.IsNullOrWhiteSpace(semester) || string.IsNullOrWhiteSpace(schoolYear))
                {
                    data.Add(new { mess = 0, message = "Please fill in all required fields." });
                    return Json(data, JsonRequestBehavior.AllowGet);
                }

                // Generate numeric student number (LONG for 10 digits)
                long studentNumber = GenerateNumericStudentNumber();
                int newStudentID = 0;

                using (var con = new SqlConnection(_conn))
                {
                    con.Open();

                    using (var cmd = new SqlCommand(
                        @"INSERT INTO Students
                    (StudentNumber, LastName, FirstName, MiddleName, Gender,
                     BirthDate, ContactNumber, Address, Email, CourseID,
                     YearLevel, Semester, SchoolYear, Status, EnrolledAt,
                     ScholarshipID, DiscountID)
                  VALUES
                    (@snum, @lname, @fname, @mname, @gender,
                     @bdate, @contact, @address, @email, @courseID,
                     @year, @sem, @sy, 'Enrolled', GETDATE(),
                     @schid, @did);
                  SELECT SCOPE_IDENTITY();", con))
                    {
                        // StudentNumber is now LONG
                        cmd.Parameters.AddWithValue("@snum", studentNumber);
                        cmd.Parameters.AddWithValue("@lname", lastName.Trim().ToUpper());
                        cmd.Parameters.AddWithValue("@fname", firstName.Trim().ToUpper());
                        cmd.Parameters.AddWithValue("@mname", string.IsNullOrEmpty(middleName) ? (object)DBNull.Value : middleName.Trim().ToUpper());
                        cmd.Parameters.AddWithValue("@gender", gender);
                        cmd.Parameters.AddWithValue("@bdate", DateTime.Parse(birthDate));
                        cmd.Parameters.AddWithValue("@contact", string.IsNullOrEmpty(contact) ? (object)DBNull.Value : contact.Trim());
                        cmd.Parameters.AddWithValue("@address", string.IsNullOrEmpty(address) ? (object)DBNull.Value : address.Trim());
                        cmd.Parameters.AddWithValue("@email", string.IsNullOrEmpty(email) ? (object)DBNull.Value : email.Trim());
                        cmd.Parameters.AddWithValue("@courseID", int.Parse(courseID));
                        cmd.Parameters.AddWithValue("@year", int.Parse(yearLevel));
                        cmd.Parameters.AddWithValue("@sem", semester);
                        cmd.Parameters.AddWithValue("@sy", schoolYear.Trim());
                        cmd.Parameters.AddWithValue("@schid", string.IsNullOrEmpty(scholarshipID) ? (object)DBNull.Value : int.Parse(scholarshipID));
                        cmd.Parameters.AddWithValue("@did", string.IsNullOrEmpty(discountID) ? (object)DBNull.Value : int.Parse(discountID));

                        newStudentID = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Generate assessments
                    GenerateAssessments(con, newStudentID, int.Parse(courseID),
                        int.Parse(yearLevel), semester, schoolYear.Trim());

                    // Create user account - Username is the student number as string
                    using (var cmd = new SqlCommand(
                        @"INSERT INTO Users (Username, PasswordHash, Role, StudentID, IsActive, CreatedAt)
                  VALUES (@user, @pass, 'Student', @sid, 1, GETDATE())", con))
                    {
                        cmd.Parameters.AddWithValue("@user", studentNumber.ToString()); // Convert LONG to string
                        cmd.Parameters.AddWithValue("@pass", studentNumber.ToString()); // Default password
                        cmd.Parameters.AddWithValue("@sid", newStudentID);
                        cmd.ExecuteNonQuery();
                    }
                }

                // Return the student number as string for display
                data.Add(new { mess = 1, studentNumber = studentNumber.ToString(), message = "Student registered successfully." });
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: " + ex.Message);
                data.Add(new { mess = 0, message = "Error: " + ex.Message });
                return Json(data, JsonRequestBehavior.AllowGet);
            }
        }

        // Generate numeric student numbers starting from 1000000000 (10 digits)
        private long GenerateNumericStudentNumber()
        {
            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                using (var cmd = new SqlCommand("SELECT ISNULL(MAX(StudentNumber), 999999999) FROM Students", con))
                {
                    long maxNumber = Convert.ToInt64(cmd.ExecuteScalar());
                    return maxNumber + 1; // Returns 1000000000, 1000000001, 1000000002, etc.
                }
            }
        }
        public ActionResult GetStudent()
        {
            string id = Request["studentID"];
            var data = new List<object>();

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT s.StudentID, s.StudentNumber, s.LastName, s.FirstName,
                         s.MiddleName, s.Gender, s.BirthDate, s.ContactNumber,
                         s.Address, s.Email, s.CourseID, s.YearLevel,
                         s.Semester, s.SchoolYear, s.Status,
                         s.ScholarshipID, s.DiscountID
                  FROM   Students s
                  WHERE  s.StudentID = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", int.Parse(id));
                con.Open();
                using (var r = cmd.ExecuteReader())
                    if (r.Read())
                        data.Add(new
                        {
                            studentID = r["StudentID"],
                            studentNumber = r["StudentNumber"],
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
                cmd.Parameters.AddWithValue("@bdate", DateTime.Parse(birthDate));
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
    int yearLevel, string semester, string schoolYear, SqlTransaction trans =null) // <-- Add trans here
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
                        if (r["SchType"] != DBNull.Value && r["SchType"].ToString() == "Percent")
                            discountAmount += totalFee * (decimal)r["SchPct"] / 100;
                        else if (r["SchType"] != DBNull.Value && r["SchType"].ToString() == "Fixed")
                            discountAmount += (decimal)r["SchAmt"];

                        if (r["DisType"] != DBNull.Value && r["DisType"].ToString() == "Percent")
                            discountAmount += totalFee * (decimal)r["DisPct"] / 100;
                        else if (r["DisType"] != DBNull.Value && r["DisType"].ToString() == "Fixed")
                            discountAmount += (decimal)r["DisAmt"];
                    }
                }
            }

            if (discountAmount > totalFee) discountAmount = totalFee;
            decimal netAssessment = Math.Max(0, totalFee - discountAmount);

            var periods = new[]
            {
        new { Period = "Prelim",     Pct = 53m, Status = "Unpaid" },
        new { Period = "Midterm",    Pct = 11m, Status = "Locked" },
        new { Period = "Semi-Final", Pct = 11m, Status = "Locked" },
        new { Period = "Final",      Pct = 25m, Status = "Locked" }
    };

            foreach (var p in periods)
            {
                int exists = 0;
                using (var cmd = new SqlCommand(@"SELECT COUNT(*) FROM Assessments
                                          WHERE StudentID=@sid AND SchoolYear=@sy AND Semester=@sem AND Period=@period", con, trans))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    cmd.Parameters.AddWithValue("@sy", schoolYear);
                    cmd.Parameters.AddWithValue("@sem", semester);
                    cmd.Parameters.AddWithValue("@period", p.Period);
                    exists = (int)cmd.ExecuteScalar();
                }
                if (exists > 0) continue;

                decimal amountDue = Math.Round(netAssessment * p.Pct / 100, 2);

                using (var cmd = new SqlCommand(@"INSERT INTO Assessments 
                    (StudentID, YearLevel, CourseFeeID, SchoolYear, Semester, Period, 
                     TotalFee, TuitionFee, RegistrationFee, MiscFee, LabFee, 
                     DiscountAmount, NetAssessment, PercentRequired, 
                     AmountDue, Status, AssessedAt) 
                  VALUES 
                    (@sid, @yl, NULL, @sy, @sem, @period, 
                     @totalFee, @tuitionFee, @regFee, @miscFee, @labFee, 
                     @discount, @net, @pct, 
                     @amountDue, @status, GETDATE())", con, trans))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    cmd.Parameters.AddWithValue("@yl", yearLevel); // <-- ADD THIS LINE
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
                    cmd.Parameters.AddWithValue("@pct", p.Pct);
                    cmd.Parameters.AddWithValue("@amountDue", amountDue);
                    cmd.Parameters.AddWithValue("@status", p.Status);
                    cmd.ExecuteNonQuery();
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

        public ActionResult Details(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            Student student = null;

            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                using (var cmd = new SqlCommand(
                    @"SELECT s.*, 
                     c.CourseCode, 
                     c.CourseName,
                     sc.ScholarshipName,
                     sc.Type as ScholarshipType,
                     sc.DiscountPercent as ScholarshipPercent,
                     sc.DiscountAmount as ScholarshipAmount,
                     d.DiscountName,
                     d.Type as DiscountType,
                     d.DiscountPercent as DiscountPercent,
                     d.DiscountAmount as DiscountAmount
              FROM Students s
              LEFT JOIN Courses c ON s.CourseID = c.CourseID
              LEFT JOIN Scholarships sc ON s.ScholarshipID = sc.ScholarshipID
              LEFT JOIN Discounts d ON s.DiscountID = d.DiscountID
              WHERE s.StudentID = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            student = new Student
                            {
                                StudentID = (int)r["StudentID"],
                                StudentNumber = Convert.ToInt64(r["StudentNumber"]),
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
                                EnrolledAt = (DateTime)r["EnrolledAt"],
                                ScholarshipID = r["ScholarshipID"] == DBNull.Value ? (int?)null : (int)r["ScholarshipID"],
                                DiscountID = r["DiscountID"] == DBNull.Value ? (int?)null : (int)r["DiscountID"]
                            };

                            // Set ViewBag values for display
                            ViewBag.CourseName = r["CourseName"] == DBNull.Value ? "Not Assigned" : r["CourseName"].ToString();
                            ViewBag.CourseCode = r["CourseCode"] == DBNull.Value ? "" : r["CourseCode"].ToString();

                            // Format scholarship display
                            if (r["ScholarshipName"] != DBNull.Value)
                            {
                                string scholarshipValue = "";
                                if (r["ScholarshipType"].ToString() == "Percent")
                                    scholarshipValue = r["ScholarshipPercent"].ToString() + "%";
                                else
                                    scholarshipValue = "₱" + Convert.ToDecimal(r["ScholarshipAmount"]).ToString("N2");

                                ViewBag.ScholarshipName = r["ScholarshipName"].ToString() + " (" + scholarshipValue + ")";
                            }
                            else
                            {
                                ViewBag.ScholarshipName = "None";
                            }

                            // Format discount display
                            if (r["DiscountName"] != DBNull.Value)
                            {
                                string discountValue = "";
                                if (r["DiscountType"].ToString() == "Percent")
                                    discountValue = r["DiscountPercent"].ToString() + "%";
                                else
                                    discountValue = "₱" + Convert.ToDecimal(r["DiscountAmount"]).ToString("N2");

                                ViewBag.DiscountName = r["DiscountName"].ToString() + " (" + discountValue + ")";
                            }
                            else
                            {
                                ViewBag.DiscountName = "None";
                            }
                        }
                    }
                }
            }

            if (student == null)
                return HttpNotFound();

            return View(student);
        }

     
    }
}