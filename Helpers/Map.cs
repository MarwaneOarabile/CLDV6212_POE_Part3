using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Models;

namespace ABCRetailers.Functions.Helpers
{
    public static class Map
    {
        public static CustomerDto ToDto(CustomerEntity entity) => new()
        {
            RowKey = entity.RowKey,
            Name = entity.Name,
            Surname = entity.Surname,
            Username = entity.Username,
            Email = entity.Email,
            ShippingAddress = entity.ShippingAddress
        };

        public static CustomerEntity ToEntity(CustomerDto dto) => new()
        {
            RowKey = dto.RowKey ?? Guid.NewGuid().ToString(),
            Name = dto.Name,
            Surname = dto.Surname,
            Username = dto.Username,
            Email = dto.Email,
            ShippingAddress = dto.ShippingAddress
        };

        public static ProductDto ToDto(ProductEntity entity) => new()
        {
            RowKey = entity.RowKey,
            ProductName = entity.ProductName,
            Description = entity.Description,
            Price = entity.Price,
            StockAvailable = entity.StockAvailable,
            ImageUrl = entity.ImageUrl
        };

        public static ProductEntity ToEntity(ProductDto dto) => new()
        {
            RowKey = dto.RowKey ?? Guid.NewGuid().ToString(),
            ProductName = dto.ProductName,
            Description = dto.Description,
            Price = dto.Price,
            StockAvailable = dto.StockAvailable,
            ImageUrl = dto.ImageUrl
        };

        public static OrderDto ToDto(OrderEntity entity) => new()
        {
            RowKey = entity.RowKey,
            CustomerId = entity.CustomerId,
            Username = entity.Username,
            ProductId = entity.ProductId,
            ProductName = entity.ProductName,
            OrderDate = entity.OrderDate,
            Quantity = entity.Quantity,
            UnitPrice = entity.UnitPrice,
            TotalPrice = entity.TotalPrice,
            Status = entity.Status
        };

        public static OrderEntity ToEntity(OrderDto dto) => new()
        {
            RowKey = dto.RowKey ?? Guid.NewGuid().ToString(),
            CustomerId = dto.CustomerId,
            Username = dto.Username,
            ProductId = dto.ProductId,
            ProductName = dto.ProductName,
            OrderDate = dto.OrderDate,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice,
            TotalPrice = dto.TotalPrice,
            Status = dto.Status
        };
    }
}