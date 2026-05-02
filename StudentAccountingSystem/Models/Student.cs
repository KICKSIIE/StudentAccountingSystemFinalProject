using System;
using System.ComponentModel.DataAnnotations;

namespace StudentAccountingSystem.Models
{
    public class Student
    {
        public int StudentID { get; set; }

        [Display(Name = "Student No.")]
        public string StudentNumber { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [StringLength(50)]
        [Display(Name = "Middle Name")]
        public string MiddleName { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Birth date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Birth Date")]
        public DateTime BirthDate { get; set; }

        [StringLength(20)]
        [Display(Name = "Contact Number")]
        public string ContactNumber { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Course is required.")]
        [Display(Name = "Course")]
        public int CourseID { get; set; }

        public string CourseCode { get; set; }
        public string CourseName { get; set; }

        [Required]
        [Display(Name = "Year Level")]
        public int YearLevel { get; set; }

        [Required]
        public string Semester { get; set; }

        [Required]
        [Display(Name = "School Year")]
        public string SchoolYear { get; set; }

        public string Status { get; set; }
        public DateTime EnrolledAt { get; set; }
    }
}