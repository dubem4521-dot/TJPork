using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TJPork.Core.Entities;
using TJPork.Infrastructure.Data;
using TJPork.Infrastructure.Services;
using Xunit;

namespace TJPork.Tests
{
    public class ProductServiceTests
    {
        private TJPorkDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<TJPorkDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new TJPorkDbContext(options);
        }

        [Fact]
        public void Product_FinalPrice_CalculatesDiscountProperly()
        {
            // Arrange
            var productWithDiscount = new Product { Price = 20.00m, DiscountPercentage = 15 };
            var productNoDiscount = new Product { Price = 25.00m, DiscountPercentage = 0 };

            // Act & Assert
            Assert.Equal(17.00m, productWithDiscount.FinalPrice);
            Assert.Equal(25.00m, productNoDiscount.FinalPrice);
        }

        [Fact]
        public async Task ProductService_Search_FiltersByTermAndCategory()
        {
            // Arrange
            var db = CreateInMemoryDbContext();
            var service = new ProductService(db);

            var cat1 = new Category { Id = 1, Name = "Bacon", Slug = "bacon" };
            var cat2 = new Category { Id = 2, Name = "Chops", Slug = "chops" };
            db.Categories.AddRange(cat1, cat2);

            var p1 = new Product { Id = 1, Name = "Applewood Bacon", Slug = "applewood-bacon", CategoryId = 1, Price = 14.99m, IsActive = true };
            var p2 = new Product { Id = 2, Name = "Maple Bacon", Slug = "maple-bacon", CategoryId = 1, Price = 16.99m, IsActive = true };
            var p3 = new Product { Id = 3, Name = "Heritage Chops", Slug = "heritage-chops", CategoryId = 2, Price = 24.99m, IsActive = true };

            db.Products.AddRange(p1, p2, p3);
            await db.SaveChangesAsync();

            // Act 1: Search by keyword
            var baconResults = await service.SearchProductsAsync("maple", null, null, null, null);
            // Act 2: Filter by category
            var categoryResults = await service.SearchProductsAsync(null, 1, null, null, null);
            // Act 3: Filter by max price
            var priceResults = await service.SearchProductsAsync(null, null, null, 15.00m, null);

            // Assert
            Assert.Single(baconResults);
            Assert.Equal("Maple Bacon", baconResults.First().Name);

            Assert.Equal(2, categoryResults.Count());

            Assert.Single(priceResults);
            Assert.Equal("Applewood Bacon", priceResults.First().Name);
        }
    }
}
