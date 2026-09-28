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

    [Theory]
    [InlineData("postgresql://postgres.ref:secret@aws-0-eu.pooler.supabase.com:6543/postgres", true)]
    [InlineData("postgres://postgres:secret@db.ref.supabase.co:5432/postgres", true)]
    [InlineData("Host=db.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=pass;", true)]
    [InlineData("Server=db.ref.supabase.co;Port=5432;Database=postgres;User Id=postgres;Password=pass;", true)]
    [InlineData("Data Source=tjpork.db", false)]
    [InlineData("Data Source=/app/data/tjpork.db", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void DbConnectionHelper_IsPostgreSql_DetectsCorrectly(string? conn, bool expected)
    {
        var actual = DbConnectionHelper.IsPostgreSql(conn);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DbConnectionHelper_FormatPostgreSqlConnectionString_FormatsUri()
    {
        var uri = "postgresql://myuser:mypassword@aws-0-eu-central-1.pooler.supabase.com:6543/postgres";
        var formatted = DbConnectionHelper.FormatPostgreSqlConnectionString(uri);

        Assert.Contains("Host=aws-0-eu-central-1.pooler.supabase.com", formatted);
        Assert.Contains("Port=6543", formatted);
        Assert.Contains("Database=postgres", formatted);
        Assert.Contains("Username=myuser", formatted);
        Assert.Contains("Password=mypassword", formatted);
        Assert.Contains("SSL Mode=Require", formatted);
    }

    [Fact]
    public async Task SupabaseFileStorageService_WithoutCredentials_SavesLocally()
    {
        var mockConfig = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        mockConfig.Setup(c => c["Supabase:Url"]).Returns("");
        mockConfig.Setup(c => c["Supabase:Key"]).Returns("");
        mockConfig.Setup(c => c["Supabase:Bucket"]).Returns("tjpork-images");

        var mockEnv = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        var tempFolder = Path.Combine(Path.GetTempPath(), "tjpork_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);
        mockEnv.Setup(e => e.WebRootPath).Returns(tempFolder);

        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<TJPork.Infrastructure.Services.SupabaseFileStorageService>>();
        var service = new TJPork.Infrastructure.Services.SupabaseFileStorageService(mockConfig.Object, mockEnv.Object, mockLogger.Object);

        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));
        var url = await service.SaveFileAsync(ms, "photo.jpg", "image/jpeg", "products");

        Assert.StartsWith("/images/products/", url);
        Assert.EndsWith(".jpg", url);

        // Cleanup
        try { Directory.Delete(tempFolder, true); } catch { }
    }
}
