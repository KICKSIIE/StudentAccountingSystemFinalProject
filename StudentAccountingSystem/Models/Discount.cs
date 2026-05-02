using System.ComponentModel.DataAnnotations;

namespace StudentAccountingSystem.Models
{
    public class Discount
    {
        public int DiscountID { get; set; }

        [Required(ErrorMessage = "Discount name is required.")]
        [StringLength(100)]
        [Display(Name = "Discount Name")]
        public string DiscountName { get; set; }

        [Required(ErrorMessage = "Type is required.")]
        [Display(Name = "Discount Type")]
        public string Type { get; set; }

        [Display(Name = "Discount Percent (%)")]
        public decimal? DiscountPercent { get; set; }

        [Display(Name = "Discount Amount (₱)")]
        public decimal? DiscountAmount { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}