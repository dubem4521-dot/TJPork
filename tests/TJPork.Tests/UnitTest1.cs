using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using TJPork.Core.Entities;
using TJPork.Core.Interfaces;
using TJPork.Infrastructure.Data;
using TJPork.Web.Controllers;
using TJPork.Web.ViewModels;
using Xunit;

namespace TJPork.Tests;

public class UnitTest1
{
    private TJPorkDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TJPorkDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new TJPorkDbContext(options);
    }

    [Fact]
    public void TestAboutUsModelDefaults()
    {
        var model = new AboutUsEditViewModel();
        Assert.Equal("/images/about/tinashe.jpg", model.Founder1ImageUrl);
        Assert.Equal("/images/about/jeffery.jpg", model.Founder2ImageUrl);
    }

    [Fact]
    public async Task HomeController_About_EmptyStoreSettings_FallsBackToDefaultImages()
    {
        var db = CreateInMemoryDbContext();
        db.StoreSettings.AddRange(
            new StoreSetting { Key = "AboutUs.Founder1ImageUrl", Value = "", Group = "AboutUs" },
            new StoreSetting { Key = "AboutUs.Founder2ImageUrl", Value = "   ", Group = "AboutUs" }
        );
        await db.SaveChangesAsync();

        var mockProductService = new Mock<IProductService>();
        var controller = new HomeController(mockProductService.Object, db);

        var result = await controller.About() as ViewResult;
        Assert.NotNull(result);
        var model = result.Model as AboutViewModel;
        Assert.NotNull(model);
        Assert.Equal("/images/about/tinashe.jpg", model.Founder1ImageUrl);
        Assert.Equal("/images/about/jeffery.jpg", model.Founder2ImageUrl);
    }

    [Fact]
    public async Task HomeController_About_CustomImages_RetainsCustomImages()
    {
        var db = CreateInMemoryDbContext();
        db.StoreSettings.AddRange(
            new StoreSetting { Key = "AboutUs.Founder1ImageUrl", Value = "/images/about/custom1.jpg", Group = "AboutUs" },
            new StoreSetting { Key = "AboutUs.Founder2ImageUrl", Value = "/images/about/custom2.jpg", Group = "AboutUs" }
        );
        await db.SaveChangesAsync();

        var mockProductService = new Mock<IProductService>();
        var controller = new HomeController(mockProductService.Object, db);

        var result = await controller.About() as ViewResult;
        Assert.NotNull(result);
        var model = result.Model as AboutViewModel;
        Assert.NotNull(model);
        Assert.Equal("/images/about/custom1.jpg", model.Founder1ImageUrl);
        Assert.Equal("/images/about/custom2.jpg", model.Founder2ImageUrl);
    }
}
