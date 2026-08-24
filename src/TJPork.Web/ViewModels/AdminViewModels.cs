using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using TJPork.Core.Entities;
using TJPork.Core.Enums;
using TJPork.Core.Models;
using TJPork.Infrastructure.Identity;

namespace TJPork.Web.ViewModels
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Owner email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;

        public string? ReturnUrl { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public DashboardSummaryDto Summary { get; set; } = new DashboardSummaryDto();
        public List<Order> RecentOrders { get; set; } = new List<Order>();
    }

    public class AdminProductListViewModel
    {
        public List<Product> Products { get; set; } = new List<Product>();
        public List<Category> Categories { get; set; } = new List<Category>();
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public string? StatusFilter { get; set; } // "all", "active", "inactive", "lowstock"
    }

    public class AdminProductEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(150)]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a category")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Short description is required (max 150 chars)")]
        [StringLength(150, ErrorMessage = "Short description cannot exceed 150 characters")]
        [Display(Name = "Short Description (Card Summary)")]
        public string ShortDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Detailed description is required")]
        [Display(Name = "Full Product Description (Butcher Notes)")]
        public string LongDescription { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 100000.00, ErrorMessage = "Price must be greater than 0")]
        [Display(Name = "Price (R)")]
        public decimal Price { get; set; }

        [Range(0, 100, ErrorMessage = "Discount must be between 0% and 100%")]
        [Display(Name = "Discount Percentage (%)")]
        public int DiscountPercentage { get; set; } = 0;

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, 10000, ErrorMessage = "Stock must be 0 or greater")]
        [Display(Name = "Stock Quantity (Units)")]
        public int StockQuantity { get; set; } = 10;

        [Display(Name = "Image URL or Path")]
        public string ImageUrl { get; set; } = "/images/products/placeholder.jpg";

        [Display(Name = "Upload New Product Image")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Featured Product (Showcase on Homepage)")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Active / Available for Sale")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Weight / Packaging Description")]
        public string WeightDescription { get; set; } = "500g pack";

        [Display(Name = "Curing & Preparation Method")]
        public string CuringMethod { get; set; } = "Hardwood smoked & dry aged";

        [Display(Name = "Origin Farm / Heritage Breed")]
        public string OriginFarm { get; set; } = "Stellenbosch Heritage Pastures";

        public List<Category> AvailableCategories { get; set; } = new List<Category>();
    }

    public class AdminOrderListViewModel
    {
        public List<Order> Orders { get; set; } = new List<Order>();
        public string? ActiveTab { get; set; } = "all";
        public string? Search { get; set; }
        public PaymentStatus? PaymentFilter { get; set; }
        public OrderStatus? StatusFilter { get; set; }
    }

    public class AdminCustomerItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
        public int TotalOrders { get; set; }
        public decimal LifetimeSpend { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public bool IsBlocked { get; set; }
        public string? Notes { get; set; }
    }

    public class AdminCustomerListViewModel
    {
        public List<AdminCustomerItemViewModel> Customers { get; set; } = new List<AdminCustomerItemViewModel>();
        public string? Search { get; set; }
    }

    public class AdminCustomerDetailViewModel
    {
        public ApplicationUser Customer { get; set; } = null!;
        public List<Order> Orders { get; set; } = new List<Order>();
        public decimal LifetimeSpend { get; set; }
        public int TotalOrdersCount { get; set; }
    }

    public class AdminAnalyticsViewModel
    {
        public DashboardSummaryDto Summary { get; set; } = new DashboardSummaryDto();
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.UtcNow;
    }

    public class AdminSettingsViewModel
    {
        [Required]
        [Display(Name = "Store Name")]
        public string StoreName { get; set; } = "T&JPork Artisanal Meats";

        [Required]
        [EmailAddress]
        [Display(Name = "Customer Support Email")]
        public string ContactEmail { get; set; } = "orders@tjpork.com";

        [Required]
        [Phone]
        [Display(Name = "Customer Support Phone")]
        public string ContactPhone { get; set; } = "+27 (0)21 835 7675";

        [Required]
        [Range(0, 1000)]
        [Display(Name = "Standard Delivery Fee (R)")]
        public decimal StandardDeliveryFee { get; set; } = 65.00m;

        [Required]
        [Range(0, 5000)]
        [Display(Name = "Free Delivery Minimum Order Threshold (R)")]
        public decimal FreeDeliveryThreshold { get; set; } = 500.00m;

        [Required]
        [Range(0, 30)]
        [Display(Name = "South African VAT Rate (%)")]
        public decimal TaxRatePercentage { get; set; } = 15.0m;

        [Required]
        [Display(Name = "Owner Notification Email Recipients (Comma-separated)")]
        public string AdminNotifyEmails { get; set; } = "tinashe@tjfork.com,jeffery@tjfork.com";

        public string? SuccessMessage { get; set; }
    }
}
