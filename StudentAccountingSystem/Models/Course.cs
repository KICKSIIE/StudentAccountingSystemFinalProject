using System.ComponentModel.DataAnnotations;

namespace StudentAccountingSystem.Models
{
    namespace StudentAccountingSystem.Models
    {
        public class Course
        {
            public int CourseID { get; set; }
            public string CourseCode { get; set; }
            public string CourseName { get; set; }
            public int Units { get; set; }
            public decimal TuitionPerUnit { get; set; }
            public decimal RegistrationFee { get; set; }
            public decimal MiscFee { get; set; }
            public decimal LabFee { get; set; }
            // Helper to calculate total
            public decimal TotalFee => (TuitionPerUnit * Units) + RegistrationFee + MiscFee + LabFee;
        }
    }
}