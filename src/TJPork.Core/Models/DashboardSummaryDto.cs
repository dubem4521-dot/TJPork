using System;
using System.Collections.Generic;

namespace TJPork.Core.Models
{
    public class DashboardSummaryDto
    {
        // Period stats
        public int TodayOrdersCount { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal WeekRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public int LowStockCount { get; set; }
        public int TotalCustomers { get; set; }

        // Lifetime Odometer (Permanent, never erased)
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int LifetimeItemsSold { get; set; }

        // Trip Meter (Tracking Interval since Admin Reset)
        public bool HasTripReset { get; set; }
        public DateTime? TripResetDate { get; set; }
        public string? TripResetBy { get; set; }
        public string? TripResetNote { get; set; }
        public int TripDaysActive { get; set; }
        public decimal TripRevenue { get; set; }
        public int TripOrdersCount { get; set; }
        public decimal TripAverageOrderValue { get; set; }
        public int TripItemsSold { get; set; }

        // Sales charts & Breakdowns
        public List<DailySalesDto> Last30DaysSales { get; set; } = new List<DailySalesDto>();
        public List<DailySalesDto> TripSalesTrajectory { get; set; } = new List<DailySalesDto>();
        public List<CategorySalesDto> CategoryBreakdown { get; set; } = new List<CategorySalesDto>();
        public List<CategorySalesDto> TripCategoryBreakdown { get; set; } = new List<CategorySalesDto>();
        public List<TopProductDto> TopSellingProducts { get; set; } = new List<TopProductDto>();
        public List<TopProductDto> TripTopSellingProducts { get; set; } = new List<TopProductDto>();
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
