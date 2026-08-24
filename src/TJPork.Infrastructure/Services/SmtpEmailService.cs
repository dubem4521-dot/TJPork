using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TJPork.Core.Entities;
using TJPork.Core.Interfaces;

namespace TJPork.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendOrderConfirmationToCustomerAsync(Order order)
        {
            var subject = $"Order Confirmation #{order.OrderNumber} - T&JPork";
            var body = new StringBuilder();
            body.AppendLine("<!DOCTYPE html><html><body style='font-family: Arial, sans-serif; background-color: #f8f9fa; padding: 20px;'>");
            body.AppendLine("<div style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 30px; border-radius: 12px; border: 1px solid #e9ecef;'>");
            body.AppendLine("<h1 style='color: #2C3E50; margin-top: 0;'>T&JPork</h1>");
            body.AppendLine($"<h2 style='color: #E67E22;'>Thank you for your order, {order.CustomerName}!</h2>");
            body.AppendLine($"<p style='color: #495057;'>We have received your order <strong>#{order.OrderNumber}</strong> and our master butchers are preparing your premium cuts.</p>");
            
            body.AppendLine("<div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin: 20px 0;'>");
            body.AppendLine($"<p style='margin: 5px 0;'><strong>Estimated Delivery:</strong> {order.DeliveryDate:MMMM dd, yyyy} ({order.DeliverySlot})</p>");
            body.AppendLine($"<p style='margin: 5px 0;'><strong>Delivery Address:</strong> {order.ShippingAddress}, {order.City} {order.PostalCode}</p>");
            body.AppendLine($"<p style='margin: 5px 0;'><strong>Payment Method:</strong> {order.PaymentMethod} ({order.PaymentStatus})</p>");
            body.AppendLine("</div>");

            body.AppendLine("<h3 style='color: #2C3E50; border-bottom: 2px solid #f0f0f0; padding-bottom: 8px;'>Order Items</h3>");
            body.AppendLine("<table style='width: 100%; border-collapse: collapse;'>");
            foreach (var item in order.Items)
            {
                body.AppendLine($"<tr><td style='padding: 8px 0; border-bottom: 1px solid #eee;'>{item.ProductName} x {item.Quantity}</td><td style='padding: 8px 0; text-align: right; border-bottom: 1px solid #eee;'>R{item.TotalPrice:F2}</td></tr>");
            }
            body.AppendLine($"<tr><td style='padding: 8px 0;'>Subtotal:</td><td style='text-align: right;'>R{order.Subtotal:F2}</td></tr>");
            body.AppendLine($"<tr><td style='padding: 8px 0;'>Delivery Fee:</td><td style='text-align: right;'>R{order.DeliveryFee:F2}</td></tr>");
            body.AppendLine($"<tr><td style='padding: 8px 0;'>VAT (15%):</td><td style='text-align: right;'>R{order.TaxAmount:F2}</td></tr>");
            if (order.DiscountAmount > 0)
            {
                body.AppendLine($"<tr><td style='padding: 8px 0; color: #27AE60;'>Discount:</td><td style='text-align: right; color: #27AE60;'>-R{order.DiscountAmount:F2}</td></tr>");
            }
            body.AppendLine($"<tr><td style='padding: 12px 0; font-weight: bold; font-size: 1.1em;'>Total:</td><td style='text-align: right; font-weight: bold; font-size: 1.1em; color: #E67E22;'>R{order.TotalAmount:F2}</td></tr>");
            body.AppendLine("</table>");

            body.AppendLine("<p style='margin-top: 30px; font-size: 0.9em; color: #6c757d; text-align: center;'>T&JPork Artisanal Meats &bull; Sourced with Care by Tinashe & Jeffery</p>");
            body.AppendLine("</div></body></html>");

            await SendGeneralEmailAsync(order.CustomerEmail, subject, body.ToString());
        }

        public async Task SendNewOrderAlertToOwnersAsync(Order order)
        {
            var subject = $"[NEW ORDER] #{order.OrderNumber} - R{order.TotalAmount:F2} ({order.CustomerName})";
            var body = new StringBuilder();
            body.AppendLine("<!DOCTYPE html><html><body style='font-family: Arial, sans-serif; background-color: #f8f9fa; padding: 20px;'>");
            body.AppendLine("<div style='max-width: 600px; margin: 0 auto; background: #ffffff; padding: 30px; border-radius: 12px; border: 1px solid #e9ecef;'>");
            body.AppendLine("<h2 style='color: #2C3E50;'>New Order Received for T&JPork!</h2>");
            body.AppendLine($"<p><strong>Customer:</strong> {order.CustomerName}</p>");
            body.AppendLine($"<p><strong>Email:</strong> {order.CustomerEmail}</p>");
            body.AppendLine($"<p><strong>Phone:</strong> {order.CustomerPhone}</p>");
            body.AppendLine($"<p><strong>Address:</strong> {order.ShippingAddress}, {order.City} {order.PostalCode}</p>");
            body.AppendLine($"<p><strong>Delivery Time:</strong> {order.DeliveryDate:MMM dd, yyyy} ({order.DeliverySlot})</p>");
            body.AppendLine($"<p><strong>Payment:</strong> {order.PaymentMethod} - {order.PaymentStatus}</p>");
            if (!string.IsNullOrEmpty(order.SpecialInstructions))
            {
                body.AppendLine($"<p><strong>Special Instructions:</strong> {order.SpecialInstructions}</p>");
            }
            body.AppendLine("<h3>Order Items:</h3><ul>");
            foreach (var item in order.Items)
            {
                body.AppendLine($"<li>{item.Quantity}x {item.ProductName} (R{item.TotalPrice:F2})</li>");
            }
            body.AppendLine("</ul>");
            body.AppendLine($"<h3>Total Revenue: R{order.TotalAmount:F2}</h3>");
            body.AppendLine("</div></body></html>");

            var ownerEmails = new[] { "tinashe@tjfork.com", "jeffery@tjfork.com" };
            foreach (var email in ownerEmails)
            {
                await SendGeneralEmailAsync(email, subject, body.ToString());
            }
        }

        public async Task SendGeneralEmailAsync(string toEmail, string subject, string bodyHtml)
        {
            try
            {
                var smtpHost = _config["EmailSettings:SmtpHost"];
                var smtpPortStr = _config["EmailSettings:SmtpPort"];
                var smtpUser = _config["EmailSettings:SmtpUser"];
                var smtpPass = _config["EmailSettings:SmtpPass"];
                var fromEmail = _config["EmailSettings:FromEmail"] ?? "orders@tjpork.com";

                if (!string.IsNullOrEmpty(smtpHost) && int.TryParse(smtpPortStr, out var smtpPort))
                {
                    using var client = new SmtpClient(smtpHost, smtpPort)
                    {
                        EnableSsl = smtpPort == 587 || smtpPort == 465,
                        Credentials = new NetworkCredential(smtpUser, smtpPass)
                    };

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, "T&JPork"),
                        Subject = subject,
                        Body = bodyHtml,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation("Sent email to {ToEmail} with subject: {Subject}", toEmail, subject);
                }
                else
                {
                    _logger.LogInformation("[MOCK EMAIL DISPATCH] To: {ToEmail} | Subject: {Subject}", toEmail, subject);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email to {ToEmail}. Fallback to log.", toEmail);
            }
        }
    }
}
