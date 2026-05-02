using System;

namespace StudentAccountingSystem.Models
{
    public class ReactivationRequest
    {
        public int RequestID { get; set; }
        public int StudentID { get; set; }
        public string StudentNumber { get; set; }
        public string StudentName { get; set; }
        public string CourseName { get; set; }
        public int YearLevel { get; set; }
        public string OldSemester { get; set; }
        public string OldSchoolYear { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; }
        public string TargetSchoolYear { get; set; }
        public string TargetSemester { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string Remarks { get; set; }
    }
}