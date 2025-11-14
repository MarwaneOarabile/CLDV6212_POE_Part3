using System.ComponentModel.DataAnnotations;

namespace ABCRetailers_ST10436124.Models.ViewModels
{
    public class CustomerOrderViewModel
    {
        [Required]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product")]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        public List<Product> Products { get; set; } = new();
    }
}