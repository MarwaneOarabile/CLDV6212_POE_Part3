using System;
using System.Collections.Generic;

namespace ABCRetailers_ST10436124.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int CustomerCount { get; set; }
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }
        public int PendingOrderCount { get; set; }
        public List<OrderSummary> RecentOrders { get; set; } = new List<OrderSummary>();
    }

    public class OrderSummary
    {
        public string OrderId { get; set; }
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public double TotalPrice { get; set; }
        public string Status { get; set; }
    }
}