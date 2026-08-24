using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using TJPork.Core.Entities;
using TJPork.Core.Enums;
using TJPork.Core.Interfaces;
using TJPork.Core.Models;
using TJPork.Infrastructure.Data;
using TJPork.Infrastructure.Services;
using Xunit;

namespace TJPork.Tests
{
    public class OrderServiceTests
    {
        private TJPorkDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<TJPorkDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new TJPorkDbContext(options);
        }

        [Fact]
        public void OrderService_GenerateOrderNumber_HasExpectedFormat()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var mockEmail = new Mock<IEmailService>();
            var service = new OrderService(db, mockEmail.Object);

            // Act
            var orderNumber = service.GenerateOrderNumber();

            // Assert
            Assert.StartsWith($"TJP-{DateTime.UtcNow.Year}-", orderNumber);
            Assert.Equal(15, orderNumber.Length); // e.g. TJP-2026-123456
        }

        [Fact]
        public async Task OrderService_CreateOrder_DecrementsInventoryAndSetsValues()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var mockEmail = new Mock<IEmailService>();
            var service = new OrderService(db, mockEmail.Object);

            var product = new Product
            {
                Id = 1,
                Name = "Applewood Smoked Bacon",
                Slug = "applewood-smoked-bacon",
                Price = 115.00m,
                StockQuantity = 20,
                IsActive = true
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var cart = new Cart();
            cart.AddItem(1, product.Name, product.Slug, product.Price, 3, "/img.jpg", "500g");

            var order = new Order
            {
                CustomerName = "Sipho Ndlovu",
                CustomerEmail = "sipho@example.co.za",
                CustomerPhone = "+27 (0)84 789 0123",
                ShippingAddress = "742 Kloof Street",
                City = "Cape Town",
                PostalCode = "8001",
                DeliveryDate = DateTime.UtcNow.AddDays(2),
                DeliverySlot = DeliverySlot.Morning,
                PaymentMethod = PaymentMethod.CreditCard
            };

            // Act
            var createdOrder = await service.CreateOrderAsync(order, cart);

            // Assert
            Assert.NotNull(createdOrder);
            Assert.Equal(PaymentStatus.Paid, createdOrder.PaymentStatus);
            Assert.Equal(OrderStatus.Processing, createdOrder.DeliveryStatus);
            Assert.Equal(345.00m, createdOrder.Subtotal);
            Assert.Single(createdOrder.Items);

            // Verify stock decrement: 20 - 3 = 17
            var updatedProduct = await db.Products.FindAsync(1);
            Assert.Equal(17, updatedProduct!.StockQuantity);

            // Verify email dispatch called
            mockEmail.Verify(e => e.SendOrderConfirmationToCustomerAsync(It.IsAny<Order>()), Times.Once);
            mockEmail.Verify(e => e.SendNewOrderAlertToOwnersAsync(It.IsAny<Order>()), Times.Once);
        }

        [Fact]
        public async Task OrderService_UpdateStatus_UpdatesAndNotifies()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var mockEmail = new Mock<IEmailService>();
            var service = new OrderService(db, mockEmail.Object);

            var order = new Order
            {
                Id = 1,
                OrderNumber = "TJP-2026-112233",
                CustomerName = "Claire Montgomery",
                CustomerEmail = "claire@example.co.za",
                CustomerPhone = "+27 (0)72 456 1122",
                ShippingAddress = "1200 Florida Rd",
                City = "Durban",
                PostalCode = "4001",
                DeliveryStatus = OrderStatus.Processing,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = PaymentMethod.CashOnDelivery
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            // Act
            var result = await service.UpdateOrderStatusAsync(1, OrderStatus.Delivered);

            // Assert
            Assert.True(result);
            var updated = await db.Orders.FindAsync(1);
            Assert.Equal(OrderStatus.Delivered, updated!.DeliveryStatus);
            Assert.Equal(PaymentStatus.Paid, updated.PaymentStatus); // COD marked paid upon delivery
            Assert.NotNull(updated.DeliveredAt);
        }
    }
}
