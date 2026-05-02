using System.ComponentModel.DataAnnotations;

namespace StudentAccountingSystem.Models
{
    public class Course
    {
        public int CourseID { get; set; }

        [Required(ErrorMessage = "Course code is required.")]
        [StringLength(20)]
        [Display(Name = "Course Code")]
        public string CourseCode { get; set; }

        [Required(ErrorMessage = "Course name is required.")]
        [StringLength(100)]
        [Display(Name = "Course Name")]
        public string CourseName { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Fee structure
        [Required(ErrorMessage = "Units is required.")]
        [Range(1, 50, ErrorMessage = "Units must be between 1 and 50.")]
        [Display(Name = "No. of Units")]
        public int Units { get; set; }

        [Required(ErrorMessage = "Tuition per unit is required.")]
        [Range(0, 99999)]
        [Display(Name = "Tuition Per Unit (₱)")]
        public decimal TuitionPerUnit { get; set; }

        [Required(ErrorMessage = "Registration fee is required.")]
        [Range(0, 99999)]
        [Display(Name = "Registration Fee (₱)")]
        public decimal RegistrationFee { get; set; }

        [Required(ErrorMessage = "Miscellaneous fee is required.")]
        [Range(0, 99999)]
        [Display(Name = "Miscellaneous Fee (₱)")]
        public decimal MiscFee { get; set; }

        [Required(ErrorMessage = "Laboratory fee is required.")]
        [Range(0, 99999)]
        [Display(Name = "Laboratory Fee (₱)")]
        public decimal LabFee { get; set; }

        // Computed — not stored, just for display
        public decimal TotalFee => (TuitionPerUnit * Units) + RegistrationFee + MiscFee + LabFee;
    }
}