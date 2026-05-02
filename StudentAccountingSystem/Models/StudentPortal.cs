using System;
using System.Collections.Generic;

namespace StudentAccountingSystem.Models
{
    public class StudentPortalDashboard
    {
        // Student info
        public string StudentNumber { get; set; } = "";
        public string FullName { get; set; } = "";
        public string CourseName { get; set; } = "";
        public int YearLevel { get; set; } = 0;
        public string Semester { get; set; } = "";
        public string SchoolYear { get; set; } = "";
        public string Status { get; set; } = "";

        public bool HasPendingWithdrawal { get; set; }
        // Lists
        public List<StudentAssessmentRow> Assessments { get; set; } = new List<StudentAssessmentRow>();
        public List<StudentDeductionRow> Deductions { get; set; } = new List<StudentDeductionRow>();
        public List<StudentPaymentRow> Payments { get; set; } = new List<StudentPaymentRow>();
        // Computed totals
        public decimal TotalAmountDue => 0; // shown per period
        public decimal TotalPaid => 0;
    }

    public class StudentAssessmentRow
    {
        public int AssessmentID { get; set; }
        public string Period { get; set; }
        public decimal TuitionFee { get; set; }
        public decimal RegistrationFee { get; set; }
        public decimal MiscFee { get; set; }
        public decimal LabFee { get; set; }
        public decimal TotalFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAssessment { get; set; }
        public decimal PercentRequired { get; set; }
        public decimal AmountDue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; }
        public bool IsLocked => Status == "Locked";
        public DateTime AssessedAt { get; set; }
        public List<StudentPaymentRow> Payments { get; set; } = new List<StudentPaymentRow>();
    }

    public class StudentDeductionRow
    {
        public string Name { get; set; }
        public string Type { get; set; }  // "Percent" or "Fixed"
        public decimal Percent { get; set; }
        public decimal FixedAmount { get; set; }
        public string Category { get; set; }  // "Scholarship" or "Discount"
    }

    public class StudentPaymentRow
    {
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; }
        public string ReferenceNumber { get; set; }
        public string Period { get; set; }
        public bool IsCarryover { get; set; }
        public string Remarks { get; set; }
    }

}