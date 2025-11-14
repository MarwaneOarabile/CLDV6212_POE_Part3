using System.ComponentModel.DataAnnotations;

namespace ABCRetailers_ST10436124.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty; // Will be hashed

        [Required]
        public string Role { get; set; } = "Customer"; // "Customer" or "Admin"

        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Shipping Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastLogin { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    }
}
