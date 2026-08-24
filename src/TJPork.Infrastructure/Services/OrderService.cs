using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TJPork.Core.Entities;
using TJPork.Core.Enums;
using TJPork.Core.Interfaces;
using TJPork.Core.Models;
using TJPork.Infrastructure.Data;

namespace TJPork.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly TJPorkDbContext _db;
        private readonly IEmailService _emailService;

        public OrderService(TJPorkDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        public string GenerateOrderNumber()
        {
            var year = DateTime.UtcNow.Year;
            var randomBytes = new byte[3];
            RandomNumberGenerator.Fill(randomBytes);
            var randomNum = Math.Abs(BitConverter.ToInt32(new byte[] { randomBytes[0], randomBytes[1], randomBytes[2], 0 }, 0)) % 900000 + 100000;
            return $"TJP-{year}-{randomNum}";
        }

        public async Task<Order> CreateOrderAsync(Order order, Cart cart)
        {
            if (string.IsNullOrWhiteSpace(order.OrderNumber))
            {
                order.OrderNumber = GenerateOrderNumber();
            }

            order.CreatedAt = DateTime.UtcNow;
            order.Subtotal = cart.Subtotal;
            order.DeliveryFee = cart.DeliveryFee;
            order.TaxAmount = cart.TaxAmount;
            order.DiscountAmount = cart.DiscountAmount;
            order.TotalAmount = cart.Total;

            if (order.PaymentMethod == PaymentMethod.CreditCard)
            {
                order.PaymentStatus = PaymentStatus.Paid; // Instant card payment
            }
            else
            {
                order.PaymentStatus = PaymentStatus.Pending; // Cash / Terminal on delivery
            }

            order.DeliveryStatus = OrderStatus.Processing;

            // Populate Order Items
            foreach (var item in cart.Items)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.TotalPrice,
                    ProductImageUrl = item.ImageUrl
                });

                // Decrement inventory
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity -= item.Quantity;
                    if (product.StockQuantity < 0) product.StockQuantity = 0;
                }
            }

            _db.Orders.Add(order);

            // Add notification for user
            if (!string.IsNullOrEmpty(order.UserId))
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = order.UserId,
                    Title = "Order Placed Successfully",
                    Message = $"Your order {order.OrderNumber} for R{order.TotalAmount:F2} has been received and is being prepared.",
                    LinkUrl = $"/Profile#orders",
                    IconClass = "fa-solid fa-bag-shopping",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            // Send notification emails
            try
            {
                await _emailService.SendOrderConfirmationToCustomerAsync(order);
                await _emailService.SendNewOrderAlertToOwnersAsync(order);
            }
            catch
            {
                // Suppress in background
            }

            return order;
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
        {
            return await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId)
        {
            return await _db.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync(OrderStatus? status = null, PaymentStatus? paymentStatus = null)
        {
            var query = _db.Orders.Include(o => o.Items).AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(o => o.DeliveryStatus == status.Value);
            }

            if (paymentStatus.HasValue)
            {
                query = query.Where(o => o.PaymentStatus == paymentStatus.Value);
            }

            return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.DeliveryStatus = status;
            if (status == OrderStatus.Delivered)
            {
                order.DeliveredAt = DateTime.UtcNow;
                if (order.PaymentMethod == PaymentMethod.CashOnDelivery)
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                }
            }

            // If user exists, create tracking notification
            if (!string.IsNullOrEmpty(order.UserId))
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = order.UserId,
                    Title = $"Order {order.OrderNumber} Updated",
                    Message = $"Your order is now marked as {status}.",
                    LinkUrl = $"/Profile#orders",
                    IconClass = status switch
                    {
                        OrderStatus.Shipped => "fa-solid fa-truck-fast",
                        OrderStatus.Delivered => "fa-solid fa-circle-check",
                        OrderStatus.Cancelled => "fa-solid fa-circle-xmark",
                        _ => "fa-solid fa-box-open"
                    },
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdatePaymentStatusAsync(int orderId, PaymentStatus status)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.PaymentStatus = status;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteOrderAsync(int orderId)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order == null) return false;

            _db.Orders.Remove(order);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
