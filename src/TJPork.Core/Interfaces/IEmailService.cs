using System.Threading.Tasks;
using TJPork.Core.Entities;

namespace TJPork.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendOrderConfirmationToCustomerAsync(Order order);
        Task SendNewOrderAlertToOwnersAsync(Order order);
        Task SendGeneralEmailAsync(string toEmail, string subject, string bodyHtml);
    }
}
