using System.Collections.Generic;

namespace ClientSide.ViewModels
{
    public class AnalyticsViewModel
    {
        public DashboardStatsDto Stats { get; set; } = new();
        public List<SalesTrendItemDto> SalesTrend { get; set; } = new();
        public List<SalesByCategoryDto> SalesByCategory { get; set; } = new();
        public List<MonthlySalesDto> MonthlySales { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<OrdersByStatusDto> OrdersByStatus { get; set; } = new();
        public string Period { get; set; } = "daily";
        public int Year { get; set; } = DateTime.Now.Year;
    }

    public class DashboardStatsDto
    {
        public decimal TotalSales { get; set; }
        public decimal MonthlySales { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public class SalesTrendItemDto
    {
        public string Date { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public decimal Sales { get; set; }
    }

    public class SalesByCategoryDto
    {
        public string Category { get; set; } = string.Empty;
        public decimal Sales { get; set; }
        public int Count { get; set; }
    }

    public class MonthlySalesDto
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Sales { get; set; }
    }

    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class OrdersByStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

