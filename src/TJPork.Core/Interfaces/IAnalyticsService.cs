using System;
using System.Threading.Tasks;
using TJPork.Core.Models;

namespace TJPork.Core.Interfaces
{
    public interface IAnalyticsService
    {
        Task<DashboardSummaryDto> GetDashboardSummaryAsync();
        Task<byte[]> GenerateRevenueReportCsvAsync(DateTime startDate, DateTime endDate);
        Task ResetTripBaselineAsync(string adminName, string? note = null);
        Task ClearTripBaselineAsync();
    }
}
