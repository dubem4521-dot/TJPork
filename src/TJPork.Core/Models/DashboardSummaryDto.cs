using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TJPork.Core.Models
{
    public class DashboardSummaryDto
    {
        public int TodayOrdersCount { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal WeekRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int LowStockCount { get; set; }
        public decimal AverageOrderValue { get; set; }

        public List<DailySalesDto> Last30DaysSales { get; set; } = new List<DailySalesDto>();
        public List<CategorySalesDto> CategoryBreakdown { get; set; } = new List<CategorySalesDto>();
        public List<TopProductDto> TopSellingProducts { get; set; } = new List<TopProductDto>();
    }

    public class DailySalesDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrdersCount { get; set; }
    }

    public class CategorySalesDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int ItemsSold { get; set; }
    }

    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}
