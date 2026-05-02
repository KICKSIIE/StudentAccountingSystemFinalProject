using System;
using System.ComponentModel.DataAnnotations;

namespace StudentAccountingSystem.Models
{
    public class PaymentViewModel
    {
        public int PaymentID { get; set; }
        public int AssessmentID { get; set; }
        public string StudentNumber { get; set; }
        public string StudentName { get; set; }
        public string CourseName { get; set; }
        public string SchoolYear { get; set; }
        public string Semester { get; set; }
        public string Period { get; set; }
        public decimal AmountDue { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public string ReferenceNumber { get; set; }
        public string Remarks { get; set; }
        public string Status { get; set; }
    }

    public class PaymentFormModel
    {
        [Required]
        public int AssessmentID { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal AmountPaid { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        public string PaymentMethod { get; set; }

        public string ReferenceNumber { get; set; }
        public string Remarks { get; set; }
    }
}