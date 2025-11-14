using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABCRetailers.Functions.Models
{
    public class CustomerDto
    {
        public string? RowKey { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
    }

    public class ProductDto
    {
        public string? RowKey { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public int StockAvailable { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class OrderDto
    {
        public string? RowKey { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
        public string Status { get; set; } = "Submitted";
    }

    public class OrderCreateDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class FileUploadDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Base64Content { get; set; } = string.Empty;
    }
}