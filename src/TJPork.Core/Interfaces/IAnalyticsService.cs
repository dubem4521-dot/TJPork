using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TJPork.Core.Models;

namespace TJPork.Core.Interfaces
{
    public interface IAnalyticsService
    {
        Task<DashboardSummaryDto> GetDashboardSummaryAsync();
        Task<byte[]> GenerateRevenueReportCsvAsync(DateTime startDate, DateTime endDate);
    }
}
