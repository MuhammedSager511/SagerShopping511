namespace webShopping.Models
{
    public class DailySalesPoint
    {
        public DateTime Date { get; set; }
        public double Total { get; set; }
    }

    public class MonthlyRevenuePoint
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public double Total { get; set; }
    }

    public class BestSellerRow
    {
        public Product? Product { get; set; }
        public int QuantitySold { get; set; }
        public double Revenue { get; set; }
    }

    public class TopCustomerRow
    {
        public string UserId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Email { get; set; } = "";
        public double TotalSpent { get; set; }
        public int OrderCount { get; set; }
    }

    public class ReportsViewModel
    {
        public List<DailySalesPoint> DailySales { get; set; } = new();
        public List<MonthlyRevenuePoint> MonthlyRevenue { get; set; } = new();
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public List<BestSellerRow> BestSellers { get; set; } = new();
        public List<Product> LowStockProducts { get; set; } = new();
        public List<TopCustomerRow> TopCustomers { get; set; } = new();
    }
}
