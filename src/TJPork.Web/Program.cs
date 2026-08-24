using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TJPork.Core.Interfaces;
using TJPork.Infrastructure.Data;
using TJPork.Infrastructure.Identity;
using TJPork.Infrastructure.Seed;
using TJPork.Infrastructure.Services;
using TJPork.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Database Context (SQLite for cross-platform ease & out-of-the-box local execution)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=tjpork.db";

builder.Services.AddDbContext<TJPorkDbContext>(options =>
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("TJPork.Infrastructure")));

// 2. Add ASP.NET Core Identity with Role Support
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password configuration
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<TJPorkDbContext>()
.AddDefaultTokenProviders();

// 3. Configure Application & Admin Cookies (30-Minute Inactivity Session Timeout)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// 4. Configure Session State for Shopping Cart
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".TJPork.Session";
});

builder.Services.AddHttpContextAccessor();

// 5. Register Domain & Infrastructure Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<ICartService, CartService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 6. Automatic Seed Database & Create Roles / Admin Accounts on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<TJPorkDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await DatabaseSeeder.SeedAsync(db, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the T&JPork database.");
    }
}

// 7. Configure HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
