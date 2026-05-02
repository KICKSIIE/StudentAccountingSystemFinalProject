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

            var list = new List<Student>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT s.StudentID, s.StudentNumber, s.LastName, s.FirstName,
                         s.MiddleName, s.Gender, s.YearLevel, s.Semester,
                         s.SchoolYear, s.Status, c.CourseCode
                  FROM   Students s
                  JOIN   Courses  c ON s.CourseID = c.CourseID
                  ORDER  BY s.LastName, s.FirstName", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new Student
                        {
                            StudentID = (int)r["StudentID"],
                            StudentNumber = r["StudentNumber"].ToString(),
                            LastName = r["LastName"].ToString(),
                            FirstName = r["FirstName"].ToString(),
                            MiddleName = r["MiddleName"].ToString(),
                            Gender = r["Gender"].ToString(),
                            YearLevel = (int)r["YearLevel"],
                            Semester = r["Semester"].ToString(),
                            SchoolYear = r["SchoolYear"].ToString(),
                            Status = r["Status"].ToString(),
                            CourseCode = r["CourseCode"].ToString()
                        });
            }
            return View(list);
        }

        public ActionResult Create()
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");
            ViewBag.Courses = GetCourses();
            ViewBag.SchoolYears = GetSchoolYears();
            ViewBag.Scholarships = GetScholarships();  // ADD
            ViewBag.Discounts = GetDiscounts();     // ADD
            return View(new Student());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Student model)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.Courses = GetCourses();
                return View(model);
            }

            string studentNumber = GenerateStudentNumber();
            int newStudentID = 0;

            using (var con = new SqlConnection(_conn))
            {
                con.Open();

                // 1 — Insert student
                // 1 — Insert student AND get new ID in one command
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
                    cmd.Parameters.AddWithValue("@snum", studentNumber);
                    cmd.Parameters.AddWithValue("@lname", model.LastName.Trim());
                    cmd.Parameters.AddWithValue("@fname", model.FirstName.Trim());
                    cmd.Parameters.AddWithValue("@mname", string.IsNullOrEmpty(model.MiddleName) ? (object)DBNull.Value : model.MiddleName.Trim());
                    cmd.Parameters.AddWithValue("@gender", model.Gender);
                    cmd.Parameters.AddWithValue("@bdate", model.BirthDate);
                    cmd.Parameters.AddWithValue("@contact", string.IsNullOrEmpty(model.ContactNumber) ? (object)DBNull.Value : model.ContactNumber.Trim());
                    cmd.Parameters.AddWithValue("@address", string.IsNullOrEmpty(model.Address) ? (object)DBNull.Value : model.Address.Trim());
                    cmd.Parameters.AddWithValue("@email", string.IsNullOrEmpty(model.Email) ? (object)DBNull.Value : model.Email.Trim());
                    cmd.Parameters.AddWithValue("@courseID", model.CourseID);
                    cmd.Parameters.AddWithValue("@year", model.YearLevel);
                    cmd.Parameters.AddWithValue("@sem", model.Semester);
                    cmd.Parameters.AddWithValue("@sy", model.SchoolYear.Trim());
                    cmd.Parameters.AddWithValue("@schid", model.ScholarshipID.HasValue ? (object)model.ScholarshipID.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@did", model.DiscountID.HasValue ? (object)model.DiscountID.Value : DBNull.Value);
                    newStudentID = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 2 — Get new StudentID
                //using (var cmd = new SqlCommand("SELECT SCOPE_IDENTITY()", con))

               

                //newStudentID = Convert.ToInt32(cmd.ExecuteScalar()); not need
                // 3 — Auto-generate 4 assessments
                GenerateAssessments(con, newStudentID, model.CourseID,
                    model.YearLevel, model.Semester, model.SchoolYear.Trim());

                // 4 — Auto-create student login
                using (var cmd = new SqlCommand(
                    @"INSERT INTO Users
                        (Username, PasswordHash, Role, StudentID, IsActive, CreatedAt)
                      VALUES
                        (@username, @password, 'Student', @sid, 1, GETDATE())", con))
                {
                    cmd.Parameters.AddWithValue("@username", studentNumber);
                    cmd.Parameters.AddWithValue("@password", studentNumber);
                    cmd.Parameters.AddWithValue("@sid", newStudentID);
                    cmd.ExecuteNonQuery();
                }
            }

            TempData["Success"] = "Student " + studentNumber +
                " registered. Login credentials: " + studentNumber;
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            Student model = null;
            using (var con = new SqlConnection(_conn))
            {
                // existing student query stays the same
                using (var cmd = new SqlCommand(
                    @"SELECT StudentID, StudentNumber, LastName, FirstName, MiddleName,
                     Gender, BirthDate, ContactNumber, Address, Email,
                     CourseID, YearLevel, Semester, SchoolYear, Status, ScholarshipID, DiscountID
              FROM   Students WHERE StudentID = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    con.Open();
                    using (var r = cmd.ExecuteReader())
                        if (r.Read())
                            model = new Student
                            {
                                StudentID = (int)r["StudentID"],
                                StudentNumber = r["StudentNumber"].ToString(),
                                LastName = r["LastName"].ToString(),
                                FirstName = r["FirstName"].ToString(),
                                MiddleName = r["MiddleName"] == DBNull.Value ? "" : r["MiddleName"].ToString(),
                                Gender = r["Gender"].ToString(),
                                BirthDate = (DateTime)r["BirthDate"],
                                ContactNumber = r["ContactNumber"] == DBNull.Value ? "" : r["ContactNumber"].ToString(),
                                Address = r["Address"] == DBNull.Value ? "" : r["Address"].ToString(),
                                Email = r["Email"] == DBNull.Value ? "" : r["Email"].ToString(),
                                CourseID = (int)r["CourseID"],
                                YearLevel = (int)r["YearLevel"],
                                Semester = r["Semester"].ToString(),
                                SchoolYear = r["SchoolYear"].ToString(),
                                Status = r["Status"].ToString(),
                                ScholarshipID = r["ScholarshipID"] == DBNull.Value ? (int?)null : (int)r["ScholarshipID"],
                                DiscountID = r["DiscountID"] == DBNull.Value ? (int?)null : (int)r["DiscountID"],
                            };
                }

                if (model == null) return HttpNotFound();
              
            }

            ViewBag.Courses = GetCourses();
            ViewBag.SchoolYears = GetSchoolYears();
            ViewBag.Scholarships = GetScholarships();
            ViewBag.Discounts = GetDiscounts();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Student model)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.Courses = GetCourses();
                ViewBag.SchoolYears = GetSchoolYears();
                ViewBag.Scholarships = GetScholarships();  // ADD
                ViewBag.Discounts = GetDiscounts();     // ADD
                return View(model);
            }

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"UPDATE Students SET
                    LastName      = @lname,
                    FirstName     = @fname,
                    MiddleName    = @mname,
                    Gender        = @gender,
                    BirthDate     = @bdate,
                    ContactNumber = @contact,
                    Address       = @address,
                    Email         = @email,
                    CourseID      = @courseID,
                    YearLevel     = @year,
                    Semester      = @sem,
                    SchoolYear    = @sy,
                    Status        = @status,
                    ScholarshipID = @schid,
                    DiscountID    = @did
                  WHERE StudentID = @id", con))
            {
                cmd.Parameters.AddWithValue("@lname", model.LastName.Trim());
                cmd.Parameters.AddWithValue("@fname", model.FirstName.Trim());
                cmd.Parameters.AddWithValue("@mname", string.IsNullOrEmpty(model.MiddleName) ? (object)DBNull.Value : model.MiddleName.Trim());
                cmd.Parameters.AddWithValue("@gender", model.Gender);
                cmd.Parameters.AddWithValue("@bdate", model.BirthDate);
                cmd.Parameters.AddWithValue("@contact", string.IsNullOrEmpty(model.ContactNumber) ? (object)DBNull.Value : model.ContactNumber.Trim());
                cmd.Parameters.AddWithValue("@address", string.IsNullOrEmpty(model.Address) ? (object)DBNull.Value : model.Address.Trim());
                cmd.Parameters.AddWithValue("@email", string.IsNullOrEmpty(model.Email) ? (object)DBNull.Value : model.Email.Trim());
                cmd.Parameters.AddWithValue("@courseID", model.CourseID);
                cmd.Parameters.AddWithValue("@year", model.YearLevel);
                cmd.Parameters.AddWithValue("@sem", model.Semester);
                cmd.Parameters.AddWithValue("@sy", model.SchoolYear.Trim());
                cmd.Parameters.AddWithValue("@status", model.Status);
                cmd.Parameters.AddWithValue("@id", model.StudentID);
                cmd.Parameters.AddWithValue("@schid", model.ScholarshipID.HasValue ? (object)model.ScholarshipID.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@did", model.DiscountID.HasValue ? (object)model.DiscountID.Value : DBNull.Value);
                con.Open();
                cmd.ExecuteNonQuery();
            }
            TempData["Success"] = "Student updated successfully.";
            return RedirectToAction("Index");
        }

        public ActionResult Details(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");

            Student model = null;
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT s.StudentID, s.StudentNumber, s.LastName, s.FirstName,
                         s.MiddleName, s.Gender, s.BirthDate, s.ContactNumber,
                         s.Address, s.Email, s.YearLevel, s.Semester,
                         s.SchoolYear, s.Status, s.EnrolledAt,
                         c.CourseCode, c.CourseName
                  FROM   Students s
                  JOIN   Courses  c ON s.CourseID = c.CourseID
                  WHERE  s.StudentID = @id", con))
            {
                cmd.Parameters.AddWithValue("@id", id);
                con.Open();
                using (var r = cmd.ExecuteReader())
                    if (r.Read())
                        model = new Student
                        {
                            StudentID = (int)r["StudentID"],
                            StudentNumber = r["StudentNumber"].ToString(),
                            LastName = r["LastName"].ToString(),
                            FirstName = r["FirstName"].ToString(),
                            MiddleName = r["MiddleName"] == DBNull.Value ? "" : r["MiddleName"].ToString(),
                            Gender = r["Gender"].ToString(),
                            BirthDate = (DateTime)r["BirthDate"],
                            ContactNumber = r["ContactNumber"] == DBNull.Value ? "" : r["ContactNumber"].ToString(),
                            Address = r["Address"] == DBNull.Value ? "" : r["Address"].ToString(),
                            Email = r["Email"] == DBNull.Value ? "" : r["Email"].ToString(),
                            CourseID = (int)r["CourseID"],
                            CourseCode = r["CourseCode"].ToString(),
                            CourseName = r["CourseName"].ToString(),
                            YearLevel = (int)r["YearLevel"],
                            Semester = r["Semester"].ToString(),
                            SchoolYear = r["SchoolYear"].ToString(),
                            Status = r["Status"].ToString(),
                            EnrolledAt = (DateTime)r["EnrolledAt"]
                        };
            }
            if (model == null) return HttpNotFound();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
                return RedirectToAction("Login", "Account");
            try
            {
                using (var con = new SqlConnection(_conn))
                {
                    con.Open();
                    using (var cmd = new SqlCommand(
                        "DELETE FROM Payments WHERE StudentID = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SqlCommand(
                        "DELETE FROM Assessments WHERE StudentID = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SqlCommand(
                        "DELETE FROM Users WHERE StudentID = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                    using (var cmd = new SqlCommand(
                        "DELETE FROM Students WHERE StudentID = @id", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                TempData["Success"] = "Student deleted successfully.";
            }
            catch
            {
                TempData["Error"] = "Could not delete student.";
            }
            return RedirectToAction("Index");
        }

        // ── CORE: Auto-generate 4 assessments ─────────────────────────
        public static void GenerateAssessments(SqlConnection con, int studentID,
    int courseID, int yearLevel, string semester, string schoolYear)
        {
            // ── Get fee rates directly from Courses table ─────────────────
            decimal tuitionPerUnit = 0, regFee = 0, miscFee = 0, labFee = 0;
            int units = 0;

            using (var cmd = new SqlCommand(
                @"SELECT Units, TuitionPerUnit, RegistrationFee, MiscFee, LabFee
          FROM Courses WHERE CourseID = @cid", con))
            {
                cmd.Parameters.AddWithValue("@cid", courseID);
                using (var r = cmd.ExecuteReader())
                {
                    if (!r.Read()) return; // no course found
                    units = (int)r["Units"];
                    tuitionPerUnit = (decimal)r["TuitionPerUnit"];
                    regFee = (decimal)r["RegistrationFee"];
                    miscFee = (decimal)r["MiscFee"];
                    labFee = (decimal)r["LabFee"];
                }
            }

            // ── Get discount from student's linked Scholarship/Discount ───
            decimal discountAmount = 0;
            decimal tuitionFee = tuitionPerUnit * units;
            decimal totalFee = tuitionFee + regFee + miscFee + labFee;
            decimal totalFeeForDiscount = totalFee;

            using (var cmd = new SqlCommand(
                @"SELECT
            sc.Type AS SchType,
            sc.DiscountPercent AS SchPct,
            sc.DiscountAmount  AS SchAmt,
            d.Type  AS DisType,
            d.DiscountPercent  AS DisPct,
            d.DiscountAmount   AS DisAmt
          FROM Students s
          LEFT JOIN Scholarships sc ON sc.ScholarshipID = s.ScholarshipID
          LEFT JOIN Discounts     d  ON d.DiscountID    = s.DiscountID
          WHERE s.StudentID = @sid", con))
            {
                cmd.Parameters.AddWithValue("@sid", studentID);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        string schType = r["SchType"].ToString();
                        if (schType == "Percent")
                            discountAmount += totalFeeForDiscount * (decimal)r["SchPct"] / 100;
                        else if (schType == "Fixed")
                            discountAmount += (decimal)r["SchAmt"];

                        string disType = r["DisType"].ToString();
                        if (disType == "Percent")
                            discountAmount += totalFeeForDiscount * (decimal)r["DisPct"] / 100;
                        else if (disType == "Fixed")
                            discountAmount += (decimal)r["DisAmt"];
                    }
                }
            }

            if (discountAmount > totalFee) discountAmount = totalFee;
            decimal netAssessment = Math.Max(0, totalFee - discountAmount);

            // ── Insert 4 assessment periods ───────────────────────────────
            var periods = new[]
            {
        new { Period = "Prelim",     Pct = 53m,  Status = "Unpaid" },
        new { Period = "Midterm",    Pct = 11m,  Status = "Locked" },
        new { Period = "Semi-Final", Pct = 11m,  Status = "Locked" },
        new { Period = "Final",      Pct = 25m,  Status = "Locked" }
    };

            foreach (var p in periods)
            {
                // Skip if assessment already exists for this period
                int exists = 0;
                using (var cmd = new SqlCommand(
                    @"SELECT COUNT(*) FROM Assessments
              WHERE StudentID=@sid AND SchoolYear=@sy
              AND Semester=@sem AND Period=@period", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    cmd.Parameters.AddWithValue("@sy", schoolYear);
                    cmd.Parameters.AddWithValue("@sem", semester);
                    cmd.Parameters.AddWithValue("@period", p.Period);
                    exists = (int)cmd.ExecuteScalar();
                }
                if (exists > 0) continue;

                decimal amountDue = Math.Round(netAssessment * p.Pct / 100, 2);

                using (var cmd = new SqlCommand(
                    @"INSERT INTO Assessments
                (StudentID, CourseFeeID, SchoolYear, Semester, Period,
                 TotalFee, TuitionFee, RegistrationFee, MiscFee, LabFee,
                 DiscountAmount, NetAssessment, PercentRequired,
                 AmountDue, Status, AssessedAt)
              VALUES
                (@sid, NULL, @sy, @sem, @period,
                 @totalFee, @tuitionFee, @regFee, @miscFee, @labFee,
                 @discount, @net, @pct,
                 @amountDue, @status, GETDATE())", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
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

        // ── CORE: Advance semester after Final is paid ─────────────────
        public static void AdvanceSemester(SqlConnection con, int studentID)
        {
            int yearLevel = 0;
            string semester = "";
            string schoolYear = "";
            int courseID = 0;

            using (var cmd = new SqlCommand(
                @"SELECT YearLevel, Semester, SchoolYear, CourseID
                  FROM   Students WHERE StudentID = @sid", con))
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

            // Determine next academic stage
            int nextYear = yearLevel;
            string nextSemester = semester;
            string nextSY = schoolYear;

            if (semester == "1st")
            {
                // 1st → 2nd, same school year
                nextSemester = "2nd";
            }
            else if (semester == "2nd")
            {
                // 2nd → next year, 1st sem, advance school year
                nextYear = yearLevel + 1;
                nextSemester = "1st";

                // Advance school year e.g. 2025-2026 → 2026-2027
                var parts = schoolYear.Split('-');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int syStart) &&
                    int.TryParse(parts[1], out int syEnd))
                {
                    nextSY = (syStart + 1) + "-" + (syEnd + 1);
                }
            }

            // Check graduation — Year 4, 2nd sem just finished
            if (yearLevel == 4 && semester == "2nd")
            {
                using (var cmd = new SqlCommand(
                    "UPDATE Students SET Status = 'Graduated' WHERE StudentID = @sid", con))
                {
                    cmd.Parameters.AddWithValue("@sid", studentID);
                    cmd.ExecuteNonQuery();
                }
                return; // no new assessments for graduates
            }

            // Update student's academic stage
            using (var cmd = new SqlCommand(
                @"UPDATE Students SET
                    YearLevel  = @year,
                    Semester   = @sem,
                    SchoolYear = @sy
                  WHERE StudentID = @sid", con))
            {
                cmd.Parameters.AddWithValue("@year", nextYear);
                cmd.Parameters.AddWithValue("@sem", nextSemester);
                cmd.Parameters.AddWithValue("@sy", nextSY);
                cmd.Parameters.AddWithValue("@sid", studentID);
                cmd.ExecuteNonQuery();
            }

            // Auto-generate 4 new assessments for next semester
            GenerateAssessments(con, studentID, courseID,
                nextYear, nextSemester, nextSY);
        }

        // ── HELPERS ────────────────────────────────────────────────────
        private List<SelectListItem> GetCourses()
        {
            var list = new List<SelectListItem>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT CourseID, CourseCode, CourseName
                  FROM   Courses WHERE IsActive = 1 ORDER BY CourseCode", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new SelectListItem
                        {
                            Value = r["CourseID"].ToString(),
                            Text = r["CourseCode"] + " — " + r["CourseName"]
                        });
            }
            return list;
        }

        private string GenerateStudentNumber()
        {
            string year = DateTime.Now.Year.ToString();
            int count = 1;

            using (var con = new SqlConnection(_conn))
            {
                con.Open();
                while (true)
                {
                    string candidate = "SN-" + year + "-" + count.ToString("D5");

                    using (var cmd = new SqlCommand(
                        "SELECT COUNT(*) FROM Students WHERE StudentNumber = @num", con))
                    {
                        cmd.Parameters.AddWithValue("@num", candidate);
                        int exists = (int)cmd.ExecuteScalar();
                        if (exists == 0)
                            return candidate; // this number is free, use it
                    }
                    count++;
                }
            }
        }

        private SelectList GetSchoolYears()
        {
            int currentYear = DateTime.Now.Year;
            var years = new List<string>();
            // Generate 5 years back and 5 years forward
            for (int y = currentYear - 2; y <= currentYear + 5; y++)
                years.Add(y + "-" + (y + 1));
            return new SelectList(years);
        }

        private List<SelectListItem> GetScholarships()
        {
            var list = new List<SelectListItem>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT ScholarshipID, ScholarshipName, Type, DiscountPercent, DiscountAmount FROM Scholarships WHERE IsActive = 1 ORDER BY ScholarshipName", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                    {
                        string val = r["Type"].ToString() == "Percent"
                            ? r["DiscountPercent"] + "%"
                            : "₱" + string.Format("{0:N2}", r["DiscountAmount"]);
                        list.Add(new SelectListItem
                        {
                            Value = r["ScholarshipID"].ToString(),
                            Text = r["ScholarshipName"] + " (" + val + ")"
                        });
                    }
            }
            return list;
        }

        private List<SelectListItem> GetDiscounts()
        {
            var list = new List<SelectListItem>();
            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                "SELECT DiscountID, DiscountName, Type, DiscountPercent, DiscountAmount FROM Discounts WHERE IsActive = 1 ORDER BY DiscountName", con))
            {
                con.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                    {
                        string val = r["Type"].ToString() == "Percent"
                            ? r["DiscountPercent"] + "%"
                            : "₱" + string.Format("{0:N2}", r["DiscountAmount"]);
                        list.Add(new SelectListItem
                        {
                            Value = r["DiscountID"].ToString(),
                            Text = r["DiscountName"] + " (" + val + ")"
                        });
                    }
            }
            return list;
        }

        public JsonResult GetDeductions(int studentID)
        {
            var scholarships = new List<string>();
            var discounts = new List<string>();

            using (var con = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(
                @"SELECT
            sc.ScholarshipName, sc.Type AS SchType,
            sc.DiscountPercent AS SchPct, sc.DiscountAmount AS SchAmt,
            d.DiscountName, d.Type AS DisType,
            d.DiscountPercent AS DisPct, d.DiscountAmount AS DisAmt
          FROM Students s
          LEFT JOIN Scholarships sc ON sc.ScholarshipID = s.ScholarshipID
          LEFT JOIN Discounts     d ON d.DiscountID     = s.DiscountID
          WHERE s.StudentID = @sid", con))
            {
                cmd.Parameters.AddWithValue("@sid", studentID);
                con.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        if (r["ScholarshipName"] != DBNull.Value)
                        {
                            string val = r["SchType"].ToString() == "Percent"
                                ? r["SchPct"] + "%"
                                : "₱" + string.Format("{0:N2}", r["SchAmt"]);
                            scholarships.Add(r["ScholarshipName"] + " (" + val + ")");
                        }

                        if (r["DiscountName"] != DBNull.Value)
                        {
                            string val = r["DisType"].ToString() == "Percent"
                                ? r["DisPct"] + "%"
                                : "₱" + string.Format("{0:N2}", r["DisAmt"]);
                            discounts.Add(r["DiscountName"] + " (" + val + ")");
                        }
                    }
                }
            }

            return Json(new { scholarships, discounts }, JsonRequestBehavior.AllowGet);
        }
    }
}