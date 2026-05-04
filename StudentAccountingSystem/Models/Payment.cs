using System;
using System.Collections.Generic;

namespace StudentAccountingSystem.Models
{
    // Used in Payment Index (cashier recording)
    public class PaymentViewModel
    {
        public int StudentID { get; set; }
        public string StudentName { get; set; }
        public string CourseName { get; set; }
        public int YearLevel { get; set; }
        public string Semester { get; set; }
        public string SchoolYear { get; set; }
        public string Status { get; set; }
        public List<AssessmentViewModel> Assessments { get; set; } = new List<AssessmentViewModel>();
    }

    // Used in Payment History
    public class PaymentHistoryRow
    {
        public int PaymentID { get; set; }
        public int ORNumber { get; set; }
        public int StudentID { get; set; }
        public string StudentName { get; set; }
        public string Period { get; set; }
        public string SchoolYear { get; set; }
        public string Semester { get; set; }
        public int YearLevel { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentMode { get; set; }
        public string TransactionType { get; set; }
        public string Remarks { get; set; }
        public DateTime PaymentDate { get; set; }
        public string ReceivedByName { get; set; }
    }

    // Used for Refund summary per semester
    public class SemesterRefundSummary
    {
        public int StudentID { get; set; }
        public string StudentName { get; set; }
        public string SchoolYear { get; set; }
        public string Semester { get; set; }
        public int YearLevel { get; set; }
        public decimal TotalPaid { get; set; }
        public bool AlreadyRefunded { get; set; }
    }
}