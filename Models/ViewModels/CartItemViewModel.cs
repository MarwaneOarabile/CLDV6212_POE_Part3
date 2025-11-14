using System.ComponentModel.DataAnnotations;

namespace ABCRetailers_ST10436124.Models.ViewModels
{
    public class CartItemViewModel
    {
        public int CartItemId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal => UnitPrice * Quantity;
        public string? ImageUrl { get; set; }
        public int StockAvailable { get; set; }

    }

    public class CartViewModel
    {
        public int CartId { get; set; }
        public List<CartItemViewModel> Items { get; set; } = [];
        public decimal TotalAmount => Items.Sum(item => item.Subtotal);
        public int TotalItems => Items.Sum(item => item.Quantity);
        public string ShippingAddress { get; set; } = string.Empty;
    }

    public class CheckoutViewModel
    {
        public CartViewModel Cart { get; set; } = new();

        [Required]
        [Display(Name = "Collection Method")]
        public string CollectionMethod { get; set; } = "Delivery";

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Online";

        [Display(Name = "Shipping Address")]
        [RequiredIf(nameof(CollectionMethod), "Delivery", ErrorMessage = "Shipping address is required for delivery")]
        public string ShippingAddress { get; set; } = string.Empty;

        public string? SpecialInstructions { get; set; }
    }

    public class RequiredIfAttribute : ValidationAttribute
    {
        private string PropertyName { get; set; }
        private object DesiredValue { get; set; }

        public RequiredIfAttribute(string propertyName, object desiredValue)
        {
            PropertyName = propertyName;
            DesiredValue = desiredValue;
        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            var instance = context.ObjectInstance;
            var type = instance.GetType();
            var propertyValue = type.GetProperty(PropertyName)?.GetValue(instance, null);

            if (propertyValue?.ToString() == DesiredValue.ToString() && (value == null || string.IsNullOrWhiteSpace(value.ToString())))
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}
