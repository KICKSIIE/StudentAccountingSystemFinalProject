using System;

namespace StudentAccountingSystem.Models
{
    public class AssessmentViewModel
    {
        public int AssessmentID { get; set; }
        public int StudentID { get; set; }
        public string StudentName { get; set; }
        public string StudentNumber { get; set; }
        public string CourseName { get; set; }
        public string SchoolYear { get; set; }
        public string Semester { get; set; }
        public string Period { get; set; }
        public decimal TotalFee { get; set; }
        public decimal TuitionFee { get; set; }
        public decimal RegistrationFee { get; set; }
        public decimal MiscFee { get; set; }
        public decimal LabFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAssessment { get; set; }
        public decimal PercentRequired { get; set; }
        public decimal AmountDue { get; set; }
        public decimal OverpaidAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; }
        public DateTime AssessedAt { get; set; }
        public int YearLevel { get; set; }
    }

    public class AssessmentFormModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int StudentID { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string SchoolYear { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string Semester { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public string Period { get; set; }
        public int YearLevel { get; set; }
    }
}