using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
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
        private readonly IHttpClientFactory _httpClientFactory;

        public SmtpEmailService(
            IConfiguration config, 
            ILogger<SmtpEmailService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        private string GetConfigValue(string primaryKey, string secondaryKey, string defaultValue = "")
        {
            return _config[primaryKey] 
                ?? _config[secondaryKey] 
                ?? defaultValue;
        }

        private string GetAdminNotificationEmail()
        {
            return GetConfigValue("ADMIN_NOTIFICATION_EMAIL", "EmailSettings:AdminEmail", "tinashe@tjpork.com");
        }

        private string GetSenderEmail()
        {
            return GetConfigValue("BREVO_SENDER_EMAIL", "EmailSettings:FromEmail", "orderstjpork@gmail.com");
        }

        private string GetSenderName()
        {
            return GetConfigValue("BREVO_SENDER_NAME", "EmailSettings:FromName", "T&JPork Artisanal Meats");
        }

        public async Task SendOrderConfirmationToCustomerAsync(Order order)
        {
            var subject = $"Order Confirmation #{order.OrderNumber} - T&JPork";
            var body = new StringBuilder();
            body.AppendLine("<!DOCTYPE html><html><body style='font-family: -apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, Helvetica, Arial, sans-serif; background-color: #F8FAFC; padding: 24px; color: #1E293B;'>");
            body.AppendLine("<div style='max-width: 620px; margin: 0 auto; background: #FFFFFF; padding: 36px; border-radius: 14px; border: 1px solid #E2E8F0; box-shadow: 0 4px 12px rgba(0,0,0,0.05);'>");
            body.AppendLine("<div style='text-align: center; margin-bottom: 24px;'>");
            body.AppendLine("<h1 style='color: #0F172A; margin: 0; font-size: 26px; font-weight: 800;'>T&amp;J<span style='color: #E05624;'>Pork</span></h1>");
            body.AppendLine("<p style='color: #64748B; font-size: 13px; margin-top: 4px; text-transform: uppercase; letter-spacing: 1px;'>Artisanal Heritage Cuts &bull; Cold-Chain Delivered</p>");
            body.AppendLine("</div>");

            body.AppendLine($"<h2 style='color: #E05624; font-size: 20px; margin-top: 0;'>Thank you for your order, {order.CustomerName}!</h2>");
            body.AppendLine($"<p style='color: #334155; line-height: 1.6;'>We have received your order <strong>#{order.OrderNumber}</strong> and our master butchers are carefully selecting and hand-packaging your cuts for scheduled delivery.</p>");

            // Payment Notice (Pay on Delivery)
            body.AppendLine("<div style='background: #FFF7ED; border-left: 4px solid #EA580C; padding: 16px; border-radius: 6px; margin: 24px 0;'>");
            body.AppendLine("<p style='margin: 0; font-size: 14px; font-weight: 700; color: #9A3412;'>💳 Payment Method: Pay Upon Delivery</p>");
            body.AppendLine("<p style='margin: 6px 0 0; font-size: 13px; color: #C2410C;'>No online payment required. Please have your card (tap/swipe POS terminal supported) or cash ready when our refrigerated courier delivers your package.</p>");
            body.AppendLine("</div>");

            // Delivery Details
            body.AppendLine("<div style='background: #F1F5F9; padding: 18px; border-radius: 8px; margin: 20px 0;'>");
            body.AppendLine($"<p style='margin: 4px 0; font-size: 14px;'><strong>📅 Estimated Delivery:</strong> {order.DeliveryDate:MMMM dd, yyyy} ({order.DeliverySlot})</p>");
            body.AppendLine($"<p style='margin: 4px 0; font-size: 14px;'><strong>📍 Delivery Address:</strong> {order.ShippingAddress}, {order.City} {order.PostalCode}</p>");
            body.AppendLine($"<p style='margin: 4px 0; font-size: 14px;'><strong>📞 Contact Phone:</strong> {order.CustomerPhone}</p>");
            if (!string.IsNullOrEmpty(order.SpecialInstructions))
            {
                body.AppendLine($"<p style='margin: 4px 0; font-size: 14px;'><strong>📝 Notes:</strong> {order.SpecialInstructions}</p>");
            }
            body.AppendLine("</div>");

            // Items Table
            body.AppendLine("<h3 style='color: #0F172A; border-bottom: 2px solid #E2E8F0; padding-bottom: 8px; margin-top: 24px;'>Order Summary</h3>");
            body.AppendLine("<table style='width: 100%; border-collapse: collapse; font-size: 14px;'>");
            foreach (var item in order.Items)
            {
                body.AppendLine($"<tr><td style='padding: 10px 0; border-bottom: 1px solid #F1F5F9;'><strong>{item.ProductName}</strong> &times; {item.Quantity}</td><td style='padding: 10px 0; text-align: right; border-bottom: 1px solid #F1F5F9; font-family: monospace;'>R{item.TotalPrice:F2}</td></tr>");
            }
            body.AppendLine($"<tr><td style='padding: 8px 0; color: #64748B;'>Subtotal:</td><td style='text-align: right; font-family: monospace;'>R{order.Subtotal:F2}</td></tr>");
            body.AppendLine($"<tr><td style='padding: 8px 0; color: #64748B;'>Refrigerated Delivery Fee:</td><td style='text-align: right; font-family: monospace;'>{(order.DeliveryFee == 0 ? "FREE" : $"R{order.DeliveryFee:F2}")}</td></tr>");
            body.AppendLine($"<tr><td style='padding: 8px 0; color: #64748B;'>VAT Included (15%):</td><td style='text-align: right; font-family: monospace;'>R{order.TaxAmount:F2}</td></tr>");
            if (order.DiscountAmount > 0)
            {
                body.AppendLine($"<tr><td style='padding: 8px 0; color: #16A34A; font-weight: 600;'>Discount Applied:</td><td style='text-align: right; color: #16A34A; font-family: monospace;'>-R{order.DiscountAmount:F2}</td></tr>");
            }
            body.AppendLine($"<tr style='font-size: 16px; font-weight: 800; border-top: 2px solid #E2E8F0;'><td style='padding: 14px 0; color: #0F172A;'>Total Due on Delivery:</td><td style='text-align: right; color: #E05624; font-family: monospace;'>R{order.TotalAmount:F2}</td></tr>");
            body.AppendLine("</table>");

            body.AppendLine("<div style='margin-top: 36px; padding-top: 20px; border-top: 1px solid #E2E8F0; text-align: center; font-size: 12px; color: #94A3B8;'>");
            body.AppendLine("<p style='margin: 0;'>T&amp;JPork Artisanal Meats &bull; Milnerton &amp; Somerset West, Cape Town</p>");
            body.AppendLine("<p style='margin: 4px 0;'>Need assistance? Contact Tinashe &amp; Jeffery directly at <strong>orderstjpork@gmail.com</strong></p>");
            body.AppendLine("</div></div></body></html>");

            await SendGeneralEmailAsync(order.CustomerEmail, subject, body.ToString());
        }

        public async Task SendNewOrderAlertToOwnersAsync(Order order)
        {
            var adminEmail = GetAdminNotificationEmail();
            var subject = $"[NEW ORDER] #{order.OrderNumber} - R{order.TotalAmount:F2} ({order.CustomerName}) [Pay on Delivery]";
            var body = new StringBuilder();
            body.AppendLine("<!DOCTYPE html><html><body style='font-family: Arial, sans-serif; background-color: #F8FAFC; padding: 20px; color: #1E293B;'>");
            body.AppendLine("<div style='max-width: 600px; margin: 0 auto; background: #FFFFFF; padding: 30px; border-radius: 12px; border: 1px solid #E2E8F0;'>");
            body.AppendLine("<h2 style='color: #E05624; margin-top: 0;'>🚀 New Customer Order Received!</h2>");
            body.AppendLine($"<p><strong>Order Number:</strong> #{order.OrderNumber}</p>");
            body.AppendLine($"<p><strong>Customer:</strong> {order.CustomerName} ({order.CustomerEmail})</p>");
            body.AppendLine($"<p><strong>Phone:</strong> {order.CustomerPhone}</p>");
            body.AppendLine($"<p><strong>Delivery Address:</strong> {order.ShippingAddress}, {order.City} {order.PostalCode}</p>");
            body.AppendLine($"<p><strong>Scheduled Slot:</strong> {order.DeliveryDate:MMMM dd, yyyy} ({order.DeliverySlot})</p>");
            body.AppendLine($"<p><strong>Payment Terms:</strong> <span style='color: #D97706; font-weight: bold;'>Pay Upon Delivery</span> (Collect R{order.TotalAmount:F2} via POS Terminal or Cash)</p>");
            if (!string.IsNullOrEmpty(order.SpecialInstructions))
            {
                body.AppendLine($"<p><strong>Customer Instructions:</strong> {order.SpecialInstructions}</p>");
            }
            body.AppendLine("<h3 style='border-top: 1px solid #E2E8F0; padding-top: 12px;'>Ordered Cuts:</h3><ul>");
            foreach (var item in order.Items)
            {
                body.AppendLine($"<li>{item.Quantity}x <strong>{item.ProductName}</strong> &mdash; R{item.TotalPrice:F2}</li>");
            }
            body.AppendLine("</ul>");
            body.AppendLine($"<div style='background: #F1F5F9; padding: 12px 16px; border-radius: 8px; font-size: 16px; font-weight: bold;'>Total Due for Collection: <span style='color: #E05624;'>R{order.TotalAmount:F2}</span></div>");
            body.AppendLine("</div></body></html>");

            await SendGeneralEmailAsync(adminEmail, subject, body.ToString());
        }

        public async Task SendWelcomeEmailAsync(string customerEmail, string customerName)
        {
            var subject = "Welcome to T&JPork Artisanal Meats!";
            var body = new StringBuilder();
            body.AppendLine("<!DOCTYPE html><html><body style='font-family: -apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, Helvetica, Arial, sans-serif; background-color: #F8FAFC; padding: 24px; color: #1E293B;'>");
            body.AppendLine("<div style='max-width: 600px; margin: 0 auto; background: #FFFFFF; padding: 36px; border-radius: 14px; border: 1px solid #E2E8F0; box-shadow: 0 4px 12px rgba(0,0,0,0.05);'>");
            body.AppendLine("<div style='text-align: center; margin-bottom: 24px;'>");
            body.AppendLine("<h1 style='color: #0F172A; margin: 0; font-size: 26px; font-weight: 800;'>T&amp;J<span style='color: #E05624;'>Pork</span></h1>");
            body.AppendLine("<p style='color: #64748B; font-size: 13px; margin-top: 4px; text-transform: uppercase; letter-spacing: 1px;'>Small-Batch Artisanal Pork &bull; Ethical Heritage Farming</p>");
            body.AppendLine("</div>");

            body.AppendLine($"<h2 style='color: #0F172A; font-size: 20px;'>Welcome to our community, {customerName}!</h2>");
            body.AppendLine("<p style='color: #334155; line-height: 1.6;'>Thank you for creating an account with T&amp;JPork. Founded by Tinashe &amp; Jeffery, we are dedicated to providing ethically raised, pasture-fed pork crafted with traditional curing techniques.</p>");

            body.AppendLine("<div style='background: #FFF7ED; padding: 20px; border-radius: 10px; margin: 24px 0; border: 1px solid #FFEDD5;'>");
            body.AppendLine("<h3 style='margin-top: 0; color: #C2410C; font-size: 16px;'>Why Choose T&amp;JPork?</h3>");
            body.AppendLine("<ul style='margin: 0; padding-left: 20px; color: #431407; font-size: 14px; line-height: 1.8;'>");
            body.AppendLine("<li><strong>Zero Hormones or Antibiotics:</strong> Naturally raised heritage breeds.</li>");
            body.AppendLine("<li><strong>Cold-Chain Refrigerated Delivery:</strong> Fresh cuts routed direct to your door.</li>");
            body.AppendLine("<li><strong>Pay on Delivery:</strong> Tap card or cash at your doorstep for 100% peace of mind.</li>");
            body.AppendLine("<li><strong>Master Butchery:</strong> Thick-cut bacon, smoked ribs, artisanal sausages &amp; roasts.</li>");
            body.AppendLine("</ul>");
            body.AppendLine("</div>");

            body.AppendLine("<div style='text-align: center; margin: 30px 0;'>");
            body.AppendLine("<a href='https://tjpork.com/Shop' style='background: #E05624; color: #FFFFFF; padding: 14px 28px; font-weight: 700; text-decoration: none; border-radius: 8px; display: inline-block;'>Shop Our Artisanal Cuts</a>");
            body.AppendLine("</div>");

            body.AppendLine("<div style='margin-top: 36px; padding-top: 20px; border-top: 1px solid #E2E8F0; text-align: center; font-size: 12px; color: #94A3B8;'>");
            body.AppendLine("<p style='margin: 0;'>T&amp;JPork Artisanal Meats &bull; Milnerton &amp; Somerset West, Cape Town</p>");
            body.AppendLine("<p style='margin: 4px 0;'>Have questions or custom catering requests? Reply directly to <strong>orderstjpork@gmail.com</strong></p>");
            body.AppendLine("</div></div></body></html>");

            await SendGeneralEmailAsync(customerEmail, subject, body.ToString());
        }

        public async Task SendAdminNewUserAlertAsync(string customerEmail, string customerName, string phone, string address, string city)
        {
            var adminEmail = GetAdminNotificationEmail();
            var subject = $"[NEW SIGN-UP] New Customer Registered: {customerName}";
            var body = new StringBuilder();
            body.AppendLine("<!DOCTYPE html><html><body style='font-family: Arial, sans-serif; background-color: #F8FAFC; padding: 20px; color: #1E293B;'>");
            body.AppendLine("<div style='max-width: 600px; margin: 0 auto; background: #FFFFFF; padding: 30px; border-radius: 12px; border: 1px solid #E2E8F0;'>");
            body.AppendLine("<h2 style='color: #0F172A; margin-top: 0;'>👤 New Customer Registration</h2>");
            body.AppendLine($"<p><strong>Full Name:</strong> {customerName}</p>");
            body.AppendLine($"<p><strong>Email Address:</strong> {customerEmail}</p>");
            body.AppendLine($"<p><strong>Phone:</strong> {phone}</p>");
            body.AppendLine($"<p><strong>Delivery Address:</strong> {address}, {city}</p>");
            body.AppendLine($"<p><strong>Registered At:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>");
            body.AppendLine("</div></body></html>");

            await SendGeneralEmailAsync(adminEmail, subject, body.ToString());
        }

        public async Task SendPasswordResetEmailAsync(string customerEmail, string customerName, string resetLink)
        {
            var subject = "Reset Your Password - T&JPork";
            var body = new StringBuilder();
            body.AppendLine("<!DOCTYPE html><html><body style='font-family: -apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, Helvetica, Arial, sans-serif; background-color: #F8FAFC; padding: 24px; color: #1E293B;'>");
            body.AppendLine("<div style='max-width: 580px; margin: 0 auto; background: #FFFFFF; padding: 36px; border-radius: 14px; border: 1px solid #E2E8F0; box-shadow: 0 4px 12px rgba(0,0,0,0.05);'>");
            body.AppendLine("<div style='text-align: center; margin-bottom: 24px;'>");
            body.AppendLine("<h1 style='color: #0F172A; margin: 0; font-size: 26px; font-weight: 800;'>T&amp;J<span style='color: #E05624;'>Pork</span></h1>");
            body.AppendLine("</div>");

            body.AppendLine($"<h2 style='color: #0F172A; font-size: 20px;'>Hello {customerName},</h2>");
            body.AppendLine("<p style='color: #334155; line-height: 1.6;'>We received a request to reset the password for your T&amp;JPork customer account. Click the button below to choose a new password:</p>");

            body.AppendLine("<div style='text-align: center; margin: 32px 0;'>");
            body.AppendLine($"<a href='{resetLink}' style='background: #E05624; color: #FFFFFF; padding: 14px 30px; font-weight: 700; text-decoration: none; border-radius: 8px; display: inline-block; font-size: 15px;'>Reset Password</a>");
            body.AppendLine("</div>");

            body.AppendLine("<p style='color: #64748B; font-size: 13px; line-height: 1.5;'>If the button above does not work, copy and paste this link into your browser:</p>");
            body.AppendLine($"<p style='word-break: break-all; font-size: 12px; color: #0284C7; font-family: monospace;'>{resetLink}</p>");

            body.AppendLine("<p style='color: #94A3B8; font-size: 13px; margin-top: 24px;'>This link is valid for 2 hours. If you did not request a password reset, you can safely ignore this email &mdash; your account remains secure.</p>");

            body.AppendLine("<div style='margin-top: 36px; padding-top: 20px; border-top: 1px solid #E2E8F0; text-align: center; font-size: 12px; color: #94A3B8;'>");
            body.AppendLine("<p style='margin: 0;'>T&amp;JPork Artisanal Meats &bull; orderstjpork@gmail.com</p>");
            body.AppendLine("</div></div></body></html>");

            await SendGeneralEmailAsync(customerEmail, subject, body.ToString());
        }

        public async Task SendFeedbackNotificationToAdminAsync(
            string senderName, 
            string senderEmail, 
            string? senderPhone, 
            string subject, 
            string message, 
            string? category = null, 
            int? rating = null)
        {
            var adminEmail = GetAdminNotificationEmail();
            var emailSubject = $"[FEEDBACK / CONTACT] {subject} - {senderName}";
            var body = new StringBuilder();
            body.AppendLine("<!DOCTYPE html><html><body style='font-family: Arial, sans-serif; background-color: #F8FAFC; padding: 20px; color: #1E293B;'>");
            body.AppendLine("<div style='max-width: 600px; margin: 0 auto; background: #FFFFFF; padding: 30px; border-radius: 12px; border: 1px solid #E2E8F0;'>");
            body.AppendLine("<h2 style='color: #E05624; margin-top: 0;'>💬 New Customer Feedback / Inquiry</h2>");
            body.AppendLine($"<p><strong>From:</strong> {senderName} (&lt;{senderEmail}&gt;)</p>");
            if (!string.IsNullOrEmpty(senderPhone))
            {
                body.AppendLine($"<p><strong>Phone:</strong> {senderPhone}</p>");
            }
            if (!string.IsNullOrEmpty(category))
            {
                body.AppendLine($"<p><strong>Topic / Category:</strong> {category}</p>");
            }
            if (rating.HasValue && rating > 0)
            {
                var stars = new string('★', rating.Value) + new string('☆', 5 - rating.Value);
                body.AppendLine($"<p><strong>Satisfaction Rating:</strong> <span style='color: #F59E0B; font-size: 18px;'>{stars}</span> ({rating}/5)</p>");
            }
            body.AppendLine($"<p><strong>Subject:</strong> {subject}</p>");
            body.AppendLine("<div style='background: #F1F5F9; padding: 16px; border-radius: 8px; margin: 16px 0; white-space: pre-wrap; font-size: 14px; line-height: 1.6;'>");
            body.AppendLine(WebUtility.HtmlEncode(message));
            body.AppendLine("</div>");
            body.AppendLine($"<p style='font-size: 12px; color: #64748B;'>Received at: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC &bull; You can reply directly to {senderEmail}</p>");
            body.AppendLine("</div></body></html>");

            await SendGeneralEmailAsync(adminEmail, emailSubject, body.ToString());
        }

        public async Task SendFeedbackConfirmationToCustomerAsync(string customerEmail, string customerName, string subject)
        {
            var emailSubject = "We received your message - T&JPork";
            var body = new StringBuilder();
            body.AppendLine("<!DOCTYPE html><html><body style='font-family: -apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, Helvetica, Arial, sans-serif; background-color: #F8FAFC; padding: 24px; color: #1E293B;'>");
            body.AppendLine("<div style='max-width: 580px; margin: 0 auto; background: #FFFFFF; padding: 36px; border-radius: 14px; border: 1px solid #E2E8F0; box-shadow: 0 4px 12px rgba(0,0,0,0.05);'>");
            body.AppendLine("<div style='text-align: center; margin-bottom: 24px;'>");
            body.AppendLine("<h1 style='color: #0F172A; margin: 0; font-size: 26px; font-weight: 800;'>T&amp;J<span style='color: #E05624;'>Pork</span></h1>");
            body.AppendLine("</div>");

            body.AppendLine($"<h2 style='color: #0F172A; font-size: 20px;'>Thank you, {customerName}!</h2>");
            body.AppendLine($"<p style='color: #334155; line-height: 1.6;'>We have received your message regarding <strong>\"{subject}\"</strong>. Tinashe and Jeffery review every customer inquiry personally, and we will get back to you as soon as possible.</p>");
            body.AppendLine("<p style='color: #334155; line-height: 1.6;'>Your feedback helps us continuously perfect our artisanal curing, pasture-raised welfare standards, and refrigerated fulfillment.</p>");

            body.AppendLine("<div style='margin-top: 36px; padding-top: 20px; border-top: 1px solid #E2E8F0; text-align: center; font-size: 12px; color: #94A3B8;'>");
            body.AppendLine("<p style='margin: 0;'>T&amp;JPork Artisanal Meats &bull; orderstjpork@gmail.com</p>");
            body.AppendLine("</div></div></body></html>");

            await SendGeneralEmailAsync(customerEmail, emailSubject, body.ToString());
        }

        public async Task SendGeneralEmailAsync(string toEmail, string subject, string bodyHtml)
        {
            // 1. Try Brevo REST API first if BREVO_API_KEY is configured
            var brevoApiKey = GetConfigValue("BREVO_API_KEY", "EmailSettings:BrevoApiKey");
            if (!string.IsNullOrWhiteSpace(brevoApiKey))
            {
                var apiSuccess = await TrySendViaBrevoApiAsync(brevoApiKey, toEmail, subject, bodyHtml);
                if (apiSuccess)
                {
                    return;
                }
                _logger.LogWarning("Brevo API delivery attempt failed. Attempting SMTP relay fallback...");
            }

            // 2. Try SMTP Relay (Brevo SMTP or configured SMTP)
            await TrySendViaSmtpAsync(toEmail, subject, bodyHtml);
        }

        private async Task<bool> TrySendViaBrevoApiAsync(string apiKey, string toEmail, string subject, string bodyHtml)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var fromEmail = GetSenderEmail();
                var fromName = GetSenderName();

                var payload = new
                {
                    sender = new { name = fromName, email = fromEmail },
                    to = new[] { new { email = toEmail } },
                    subject = subject,
                    htmlContent = bodyHtml
                };

                var json = JsonSerializer.Serialize(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                request.Headers.Add("api-key", apiKey);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("[BREVO API] Email successfully dispatched to {ToEmail} | Subject: {Subject}", toEmail, subject);
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("[BREVO API] Returned status {StatusCode}: {Error}", response.StatusCode, errorContent);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[BREVO API] Exception while sending email to {ToEmail}", toEmail);
                return false;
            }
        }

        private async Task TrySendViaSmtpAsync(string toEmail, string subject, string bodyHtml)
        {
            try
            {
                var smtpHost = GetConfigValue("BREVO_SMTP_HOST", "EmailSettings:SmtpHost", "smtp-relay.brevo.com");
                var smtpPortStr = GetConfigValue("BREVO_SMTP_PORT", "EmailSettings:SmtpPort", "587");
                var smtpUser = GetConfigValue("BREVO_SMTP_USER", "EmailSettings:SmtpUser");
                var smtpPass = GetConfigValue("BREVO_SMTP_KEY", "EmailSettings:SmtpPass");
                var fromEmail = GetSenderEmail();
                var fromName = GetSenderName();

                if (!string.IsNullOrEmpty(smtpHost) && 
                    int.TryParse(smtpPortStr, out var smtpPort) && 
                    !string.IsNullOrEmpty(smtpUser) && 
                    !string.IsNullOrEmpty(smtpPass))
                {
                    using var client = new SmtpClient(smtpHost, smtpPort)
                    {
                        EnableSsl = smtpPort == 587 || smtpPort == 465,
                        Credentials = new NetworkCredential(smtpUser, smtpPass),
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        Timeout = 15000
                    };

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = subject,
                        Body = bodyHtml,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(toEmail);

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation("[BREVO SMTP] Sent email to {ToEmail} with subject: {Subject}", toEmail, subject);
                }
                else
                {
                    _logger.LogInformation("[MOCK EMAIL DISPATCH] Brevo credentials not fully set. Mock sent to: {ToEmail} | Subject: {Subject}", toEmail, subject);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[BREVO SMTP] Failed to dispatch email to {ToEmail}. Fallback logged.", toEmail);
            }
        }
    }
}
