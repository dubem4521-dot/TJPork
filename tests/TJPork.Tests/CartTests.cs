using System;
using TJPork.Core.Models;
using Xunit;

namespace TJPork.Tests
{
    public class CartTests
    {
        [Fact]
        public void Cart_AddItem_CalculatesCorrectSubtotalAndItemCount()
        {
            // Arrange
            var cart = new Cart();

            // Act
            cart.AddItem(1, "Applewood Smoked Bacon", "applewood-bacon", 115.00m, 2, "/img1.jpg", "400g");
            cart.AddItem(2, "Heritage Pork Chops", "heritage-chops", 185.00m, 1, "/img2.jpg", "650g");

            // Assert
            Assert.Equal(3, cart.TotalItemCount);
            Assert.Equal(415.00m, cart.Subtotal); // (115 * 2) + 185 = 230 + 185 = 415.00
        }

        [Fact]
        public void Cart_SubtotalUnder500_AppliesDeliveryFee()
        {
            // Arrange
            var cart = new Cart();

            // Act
            cart.AddItem(1, "Artisanal Bratwurst", "bratwurst", 95.00m, 2, "/img.jpg", "500g");

            // Assert
            Assert.Equal(190.00m, cart.Subtotal);
            Assert.Equal(65.00m, cart.DeliveryFee);
            Assert.Equal(28.50m, cart.TaxAmount); // 15% of 190.00 = 28.50
            Assert.Equal(283.50m, cart.Total); // 190.00 + 65.00 + 28.50
        }

        [Fact]
        public void Cart_Subtotal500OrOver_QualifiesForFreeDelivery()
        {
            // Arrange
            var cart = new Cart();

            // Act
            cart.AddItem(1, "Tomahawk Pork Chop", "tomahawk", 275.00m, 2, "/img.jpg", "750g");

            // Assert
            Assert.Equal(550.00m, cart.Subtotal);
            Assert.Equal(0.00m, cart.DeliveryFee);
            Assert.Equal(82.50m, cart.TaxAmount); // 15% of 550.00
            Assert.Equal(632.50m, cart.Total);
        }

        [Fact]
        public void Cart_UpdateQuantity_RemovesItemWhenZero()
        {
            // Arrange
            var cart = new Cart();
            cart.AddItem(1, "Bacon", "bacon", 115.00m, 2, "/img.jpg", "500g");

            // Act
            cart.UpdateQuantity(1, 0);

            // Assert
            Assert.Empty(cart.Items);
            Assert.Equal(0m, cart.Subtotal);
        }

        [Fact]
        public void Cart_RemoveItem_RemovesSpecificProduct()
        {
            // Arrange
            var cart = new Cart();
            cart.AddItem(1, "Bacon", "bacon", 115.00m, 1, "/img.jpg", "500g");
            cart.AddItem(2, "Sausage", "sausage", 95.00m, 1, "/img.jpg", "500g");

            // Act
            cart.RemoveItem(1);

            // Assert
            Assert.Single(cart.Items);
            Assert.Equal(2, cart.Items[0].ProductId);
            Assert.Equal(95.00m, cart.Subtotal);
        }
    }
}
