using System.ComponentModel.DataAnnotations;


namespace ABCRetailers_ST10436124.Models
{
    public class FileUploadModel
    {
        [Required]
        [Display(Name = "Proof of Payment")]
        public IFormFile? ProofOfPayment { get; set; }

        [Display(Name = "Order ID")]
        public string? OrderId { get; set; }

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }

        public string? CustomerId { get; set; }


        public List<Customer>? Customers { get; set; }
        public List<Order>? Orders { get; set; }

    }
}
