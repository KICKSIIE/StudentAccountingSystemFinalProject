using System;

namespace StudentAccountingSystem.Models
{
    public class WithdrawalRequest
    {
        public int RequestID { get; set; }
        public int StudentID { get; set; }
        public string StudentNumber { get; set; }
        public string StudentName { get; set; }
        public string CourseName { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public decimal PenaltyPercent { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal PenaltyAmount { get; set; }
        public decimal RefundableAmount { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string Remarks { get; set; }
    }
}