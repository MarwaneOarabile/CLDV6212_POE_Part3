//St10436124
//POE PART 1 CLDV6212

/*
  Model: Order.cs
 */

using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers_ST10436124.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        
        public DateTimeOffset? Timestamp { get; set; }
        
        public ETag ETag { get; set; }

        [Display(Name = "Order ID")]
        public string OrderId => RowKey;

        [Required]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product")]
        public string ProductId { get; set; } = string.Empty;

        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Display(Name = "Unit Price")]
        [DataType(DataType.Currency)]
        public double UnitPrice { get; set; }

        [Display(Name = "Total Price")]
        [DataType(DataType.Currency)]
        public double TotalPrice { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Submitted";

        public enum OrderStatus
        {
            Submitted, //order first created
            Processing, //open/reviews order
            Completed, 
            Cancelled
        }
    }
}
