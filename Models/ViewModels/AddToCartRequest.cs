namespace ABCRetailers_ST10436124.Models.ViewModels
{
    public class AddToCartRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }
}