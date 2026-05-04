using System;
using System.Collections.Generic;

namespace StudentAccountingSystem.Models
{
    public class Student
    {
        public int StudentID { get; set; }  // this IS the student number now
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Gender { get; set; }
        public DateTime BirthDate { get; set; }
        public string ContactNumber { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public int CourseID { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int YearLevel { get; set; }
        public string Semester { get; set; }
        public string SchoolYear { get; set; }
        public string Status { get; set; }
        public DateTime EnrolledAt { get; set; }
        public int? ScholarshipID { get; set; }
        public int? DiscountID { get; set; }
    }
}