using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailers_ST10436124.Models
{
    public class Cart
    {


        [Key]
        public int CartId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Status { get; set; } = "Active"; // Active, CheckedOut, Abandoned

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<CartItem> CartItems { get; set; } = []; // ✅ Simplified collection initialization


        // Computed properties
        [NotMapped]
        public decimal TotalAmount => CartItems.Sum(item => item.Subtotal);

        [NotMapped]
        public int TotalItems => CartItems.Sum(item => item.Quantity);

    }
}
