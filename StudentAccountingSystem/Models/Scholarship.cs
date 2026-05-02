using System.ComponentModel.DataAnnotations;

namespace StudentAccountingSystem.Models
{
    public class Scholarship
    {
        public int ScholarshipID { get; set; }

        [Required(ErrorMessage = "Scholarship name is required.")]
        [StringLength(100)]
        [Display(Name = "Scholarship Name")]
        public string ScholarshipName { get; set; }

        [Required(ErrorMessage = "Type is required.")]
        [Display(Name = "Discount Type")]
        public string Type { get; set; }

        [Display(Name = "Discount Percent (%)")]
        public decimal? DiscountPercent { get; set; }

        [Display(Name = "Discount Amount (₱)")]
        public decimal? DiscountAmount { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}