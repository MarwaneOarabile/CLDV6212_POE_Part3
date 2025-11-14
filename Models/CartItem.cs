using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailers_ST10436124.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }

        [Required]
        public int CartId { get; set; }

        [Required]
        public string ProductId { get; set; } = string.Empty; // From Azure Table Storage

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        public DateTime AddedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("CartId")]
        public virtual Cart Cart { get; set; } = null!;

        // Computed property
        [NotMapped]
        public decimal Subtotal => UnitPrice * Quantity;
    }
}
