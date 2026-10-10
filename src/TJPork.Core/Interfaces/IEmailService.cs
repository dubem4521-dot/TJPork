using System.Threading.Tasks;
using TJPork.Core.Entities;

namespace TJPork.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendOrderConfirmationToCustomerAsync(Order order);
        Task SendNewOrderAlertToOwnersAsync(Order order);
        Task SendWelcomeEmailAsync(string customerEmail, string customerName);
        Task SendAdminNewUserAlertAsync(string customerEmail, string customerName, string phone, string address, string city);
        Task SendPasswordResetEmailAsync(string customerEmail, string customerName, string resetLink);
        Task SendFeedbackNotificationToAdminAsync(string senderName, string senderEmail, string? senderPhone, string subject, string message, string? category = null, int? rating = null);
        Task SendFeedbackConfirmationToCustomerAsync(string customerEmail, string customerName, string subject);
        Task SendGeneralEmailAsync(string toEmail, string subject, string bodyHtml);
    }
}
