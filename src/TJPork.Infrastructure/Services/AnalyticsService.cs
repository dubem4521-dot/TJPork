using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TJPork.Core.Enums;
using TJPork.Core.Interfaces;
using TJPork.Core.Models;
using TJPork.Infrastructure.Data;
using TJPork.Infrastructure.Identity;

namespace TJPork.Infrastructure.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly TJPorkDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnalyticsService(TJPorkDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            var now = DateTime.UtcNow;
            var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
            var weekStart = todayStart.AddDays(-(int)todayStart.DayOfWeek);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var validOrdersQuery = _db.Orders.Where(o => o.DeliveryStatus != OrderStatus.Cancelled);

            var totalRevenue = await validOrdersQuery.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var totalOrdersCount = await validOrdersQuery.CountAsync();
            var totalCustomers = await _userManager.Users.CountAsync();
            var lowStockCount = await _db.Products.CountAsync(p => p.StockQuantity <= 10 && p.IsActive);

            var todayRevenue = await validOrdersQuery.Where(o => o.CreatedAt >= todayStart).SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var todayOrdersCount = await validOrdersQuery.Where(o => o.CreatedAt >= todayStart).CountAsync();
            var weekRevenue = await validOrdersQuery.Where(o => o.CreatedAt >= weekStart).SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var monthRevenue = await validOrdersQuery.Where(o => o.CreatedAt >= monthStart).SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var avgOrderValue = totalOrdersCount > 0 ? totalRevenue / totalOrdersCount : 0m;

            // 30 Days Sales - fetch only 30 days range
            var thirtyDaysAgo = todayStart.AddDays(-29);
            var recent30DayOrders = await validOrdersQuery
                .Where(o => o.CreatedAt >= thirtyDaysAgo)
                .Select(o => new { o.CreatedAt, o.TotalAmount })
                .ToListAsync();

            var dailySalesList = new List<DailySalesDto>();
            for (int i = 29; i >= 0; i--)
            {
                var day = todayStart.AddDays(-i);
                var nextDay = day.AddDays(1);
                var dayOrders = recent30DayOrders.Where(o => o.CreatedAt >= day && o.CreatedAt < nextDay).ToList();

                dailySalesList.Add(new DailySalesDto
                {
                    Date = day.ToString("MMM dd"),
                    Revenue = dayOrders.Sum(o => o.TotalAmount),
                    OrdersCount = dayOrders.Count
                });
            }

            // Top Products
            var orderItems = await _db.OrderItems
                .Include(oi => oi.Product)
                .ThenInclude(p => p!.Category)
                .ToListAsync();

            var topProducts = orderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new TopProductDto
                {
                    ProductId = g.Key,
                    ProductName = g.First().ProductName,
                    CategoryName = g.First().Product?.Category?.Name ?? "Pork Cuts",
                    UnitsSold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.TotalPrice),
                    ImageUrl = g.First().ProductImageUrl ?? "/images/products/placeholder.jpg"
                })
                .OrderByDescending(tp => tp.UnitsSold)
                .Take(5)
                .ToList();

            // Category Breakdown
            var categoryBreakdown = orderItems
                .GroupBy(oi => oi.Product?.Category?.Name ?? "Other Cuts")
                .Select(g => new CategorySalesDto
                {
                    CategoryName = g.Key,
                    Revenue = g.Sum(x => x.TotalPrice),
                    ItemsSold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(c => c.Revenue)
                .ToList();

            return new DashboardSummaryDto
            {
                TodayOrdersCount = todayOrdersCount,
                TodayRevenue = todayRevenue,
                WeekRevenue = weekRevenue,
                MonthRevenue = monthRevenue,
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrdersCount,
                TotalCustomers = totalCustomers,
                LowStockCount = lowStockCount,
                AverageOrderValue = avgOrderValue,
                Last30DaysSales = dailySalesList,
                TopSellingProducts = topProducts,
                CategoryBreakdown = categoryBreakdown
            };
        }

        public async Task<byte[]> GenerateRevenueReportCsvAsync(DateTime startDate, DateTime endDate)
        {
            var endOfDay = endDate.Date.AddDays(1).AddTicks(-1);
            var orders = await _db.Orders
                .Include(o => o.Items)
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endOfDay)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Order Number,Date (UTC),Customer Name,Customer Email,Customer Phone,Items Count,Subtotal (R),VAT 15% (R),Delivery Fee (R),Discount (R),Total Amount (R),Payment Method,Payment Status,Delivery Status");

            foreach (var o in orders)
            {
                var dateStr = o.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                var itemsCount = o.Items.Sum(i => i.Quantity);
                var line = $"\"{o.OrderNumber}\",\"{dateStr}\",\"{EscapeCsv(o.CustomerName)}\",\"{EscapeCsv(o.CustomerEmail)}\",\"{EscapeCsv(o.CustomerPhone)}\",{itemsCount},{o.Subtotal:F2},{o.TaxAmount:F2},{o.DeliveryFee:F2},{o.DiscountAmount:F2},{o.TotalAmount:F2},\"{o.PaymentMethod}\",\"{o.PaymentStatus}\",\"{o.DeliveryStatus}\"";
                sb.AppendLine(line);
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }
    }
}
