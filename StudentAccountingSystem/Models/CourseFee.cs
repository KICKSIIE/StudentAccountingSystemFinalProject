using System.ComponentModel.DataAnnotations;

namespace StudentAccountingSystem.Models
{
    public class CourseFeeViewModel
    {
        public int CourseFeeID { get; set; }
        public string CourseName { get; set; }
        public string SchoolYear { get; set; }
        public string Semester { get; set; }
        public decimal TuitionPerUnit { get; set; }
        public decimal RegistrationFee { get; set; }
        public decimal MiscellaneousFee { get; set; }
        public decimal LaboratoryFee { get; set; }
        public int Units { get; set; }
        public decimal TotalFee { get; set; }
    }

    public class CourseFeeFormModel
    {
        public int CourseFeeID { get; set; }

        [Required]
        public int CourseID { get; set; }

        [Required]
        public string SchoolYear { get; set; }

        [Required]
        public string Semester { get; set; }

        [Required, Range(1, 10000)]
        public decimal TuitionPerUnit { get; set; }

        [Required, Range(0, 99999)]
        public decimal RegistrationFee { get; set; }

        [Required, Range(0, 99999)]
        public decimal MiscellaneousFee { get; set; }

        [Required, Range(0, 99999)]
        public decimal LaboratoryFee { get; set; }

        [Required, Range(1, 50)]
        public int Units { get; set; }
    }
}