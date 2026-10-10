using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TJPork.Core.Entities;
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

            // 1. Lifetime Odometer (Permanent, all historical non-cancelled orders)
            var totalRevenue = await validOrdersQuery.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var totalOrdersCount = await validOrdersQuery.CountAsync();
            var totalCustomers = await _userManager.Users.CountAsync();
            var lowStockCount = await _db.Products.CountAsync(p => p.StockQuantity <= 10 && p.IsActive);
            var lifetimeItemsSold = await _db.OrderItems
                .Where(oi => oi.Order != null && oi.Order.DeliveryStatus != OrderStatus.Cancelled)
                .SumAsync(oi => (int?)oi.Quantity) ?? 0;

            var todayRevenue = await validOrdersQuery.Where(o => o.CreatedAt >= todayStart).SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var todayOrdersCount = await validOrdersQuery.Where(o => o.CreatedAt >= todayStart).CountAsync();
            var weekRevenue = await validOrdersQuery.Where(o => o.CreatedAt >= weekStart).SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            var monthRevenue = await validOrdersQuery.Where(o => o.CreatedAt >= monthStart).SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var avgOrderValue = totalOrdersCount > 0 ? totalRevenue / totalOrdersCount : 0m;

            // 2. Fetch Trip Meter Settings
            var tripResetSetting = await _db.StoreSettings.FirstOrDefaultAsync(s => s.Key == "Analytics.TripResetDate");
            var tripBySetting = await _db.StoreSettings.FirstOrDefaultAsync(s => s.Key == "Analytics.TripResetBy");
            var tripNoteSetting = await _db.StoreSettings.FirstOrDefaultAsync(s => s.Key == "Analytics.TripResetNote");

            bool hasTripReset = false;
            DateTime tripResetDate = DateTime.MinValue;
            string? tripResetBy = tripBySetting?.Value;
            string? tripResetNote = tripNoteSetting?.Value;

            if (tripResetSetting != null && DateTime.TryParse(tripResetSetting.Value, null, DateTimeStyles.RoundtripKind, out var parsedDate))
            {
                hasTripReset = true;
                tripResetDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            }

            // 3. Trip Meter Calculations
            decimal tripRevenue = totalRevenue;
            int tripOrdersCount = totalOrdersCount;
            decimal tripAvgOrderValue = avgOrderValue;
            int tripItemsSold = lifetimeItemsSold;
            int tripDaysActive = 0;

            if (hasTripReset)
            {
                var tripOrdersQuery = validOrdersQuery.Where(o => o.CreatedAt >= tripResetDate);
                tripRevenue = await tripOrdersQuery.SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
                tripOrdersCount = await tripOrdersQuery.CountAsync();
                tripAvgOrderValue = tripOrdersCount > 0 ? tripRevenue / tripOrdersCount : 0m;
                tripItemsSold = await _db.OrderItems
                    .Where(oi => oi.Order != null && oi.Order.DeliveryStatus != OrderStatus.Cancelled && oi.Order.CreatedAt >= tripResetDate)
                    .SumAsync(oi => (int?)oi.Quantity) ?? 0;
                tripDaysActive = Math.Max(0, (int)(DateTime.UtcNow.Date - tripResetDate.Date).TotalDays);
            }

            // 4. Last 30 Days Sales
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

            // 5. Trip Sales Trajectory (Days since trip reset)
            var tripSalesList = new List<DailySalesDto>();
            if (hasTripReset)
            {
                var tripOrders = await validOrdersQuery
                    .Where(o => o.CreatedAt >= tripResetDate)
                    .Select(o => new { o.CreatedAt, o.TotalAmount })
                    .ToListAsync();

                var startDay = tripResetDate.Date;
                var totalDaysToDisplay = Math.Min(30, Math.Max(1, (int)(todayStart - startDay).TotalDays + 1));
                for (int i = totalDaysToDisplay - 1; i >= 0; i--)
                {
                    var day = todayStart.AddDays(-i);
                    var nextDay = day.AddDays(1);
                    var dayOrders = tripOrders.Where(o => o.CreatedAt >= day && o.CreatedAt < nextDay).ToList();

                    tripSalesList.Add(new DailySalesDto
                    {
                        Date = day.ToString("MMM dd"),
                        Revenue = dayOrders.Sum(o => o.TotalAmount),
                        OrdersCount = dayOrders.Count
                    });
                }
            }
            else
            {
                tripSalesList = dailySalesList;
            }

            // 6. Top Products & Category Breakdown (Lifetime & Trip)
            var allOrderItems = await _db.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .ThenInclude(p => p!.Category)
                .Where(oi => oi.Order != null && oi.Order.DeliveryStatus != OrderStatus.Cancelled)
                .ToListAsync();

            var topProducts = allOrderItems
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

            var categoryBreakdown = allOrderItems
                .GroupBy(oi => oi.Product?.Category?.Name ?? "Other Cuts")
                .Select(g => new CategorySalesDto
                {
                    CategoryName = g.Key,
                    Revenue = g.Sum(x => x.TotalPrice),
                    ItemsSold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(c => c.Revenue)
                .ToList();

            // Trip Top Products & Categories
            var tripOrderItems = hasTripReset 
                ? allOrderItems.Where(oi => oi.Order!.CreatedAt >= tripResetDate).ToList()
                : allOrderItems;

            var tripTopProducts = tripOrderItems
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

            var tripCategoryBreakdown = tripOrderItems
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
                LifetimeItemsSold = lifetimeItemsSold,

                HasTripReset = hasTripReset,
                TripResetDate = hasTripReset ? tripResetDate : null,
                TripResetBy = tripResetBy,
                TripResetNote = tripResetNote,
                TripDaysActive = tripDaysActive,
                TripRevenue = tripRevenue,
                TripOrdersCount = tripOrdersCount,
                TripAverageOrderValue = tripAvgOrderValue,
                TripItemsSold = tripItemsSold,

                Last30DaysSales = dailySalesList,
                TripSalesTrajectory = tripSalesList,
                TopSellingProducts = topProducts,
                TripTopSellingProducts = tripTopProducts,
                CategoryBreakdown = categoryBreakdown,
                TripCategoryBreakdown = tripCategoryBreakdown
            };
        }

        public async Task ResetTripBaselineAsync(string adminName, string? note = null)
        {
            var nowStr = DateTime.UtcNow.ToString("O");
            await UpsertSettingAsync("Analytics.TripResetDate", nowStr, "Timestamp of last analytics trip meter reset", "Analytics");
            await UpsertSettingAsync("Analytics.TripResetBy", adminName, "Admin who performed the last trip reset", "Analytics");
            await UpsertSettingAsync("Analytics.TripResetNote", note ?? "Manual Reset", "Optional note/label for current tracking cycle", "Analytics");
            await _db.SaveChangesAsync();
        }

        public async Task ClearTripBaselineAsync()
        {
            var keysToRemove = new[] { "Analytics.TripResetDate", "Analytics.TripResetBy", "Analytics.TripResetNote" };
            var settings = await _db.StoreSettings.Where(s => keysToRemove.Contains(s.Key)).ToListAsync();
            if (settings.Any())
            {
                _db.StoreSettings.RemoveRange(settings);
                await _db.SaveChangesAsync();
            }
        }

        private async Task UpsertSettingAsync(string key, string value, string description, string group)
        {
            var existing = await _db.StoreSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (existing != null)
            {
                existing.Value = value;
                existing.Description = description;
                existing.Group = group;
            }
            else
            {
                _db.StoreSettings.Add(new StoreSetting
                {
                    Key = key,
                    Value = value,
                    Description = description,
                    Group = group
                });
            }
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
