using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TJPork.Core.Entities;
using TJPork.Core.Enums;
using TJPork.Core.Interfaces;
using TJPork.Infrastructure.Data;
using TJPork.Infrastructure.Identity;
using TJPork.Web.ViewModels;

namespace TJPork.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly TJPorkDbContext _db;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly IAnalyticsService _analyticsService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;

        public AdminController(
            TJPorkDbContext db,
            IProductService productService,
            IOrderService orderService,
            IAnalyticsService analyticsService,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment env)
        {
            _db = db;
            _productService = productService;
            _orderService = orderService;
            _analyticsService = analyticsService;
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
        }

        #region Authentication

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            {
                return RedirectToAction(nameof(Index));
            }

            return View(new AdminLoginViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                ModelState.AddModelError(string.Empty, "Unauthorized access. Only store owners (Tinashe & Jeffery) can access this dashboard.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Invalid owner credentials. Please try again.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        #endregion

        #region 1. Overview Dashboard

        public async Task<IActionResult> Index()
        {
            var summary = await _analyticsService.GetDashboardSummaryAsync();
            var recentOrders = await _db.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .Take(7)
                .ToListAsync();

            var viewModel = new AdminDashboardViewModel
            {
                Summary = summary,
                RecentOrders = recentOrders
            };

            return View(viewModel);
        }

        #endregion

        #region 2. Product Management (CRUD)

        public async Task<IActionResult> Products(string? search, int? categoryId, string? status)
        {
            var query = _db.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower().Trim();
                query = query.Where(p => p.Name.ToLower().Contains(term) || p.ShortDescription.ToLower().Contains(term));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (status == "active")
            {
                query = query.Where(p => p.IsActive);
            }
            else if (status == "inactive")
            {
                query = query.Where(p => !p.IsActive);
            }
            else if (status == "lowstock")
            {
                query = query.Where(p => p.StockQuantity <= 10);
            }

            var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            var categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();

            var viewModel = new AdminProductListViewModel
            {
                Products = products,
                Categories = categories,
                Search = search,
                CategoryId = categoryId,
                StatusFilter = status ?? "all"
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            var categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
            var model = new AdminProductEditViewModel
            {
                AvailableCategories = categories,
                StockQuantity = 25,
                Price = 19.99m,
                IsActive = true
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(AdminProductEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableCategories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
                return View(model);
            }

            string imageUrl = model.ImageUrl;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using var fileStream = new FileStream(filePath, FileMode.Create);
                await model.ImageFile.CopyToAsync(fileStream);
                imageUrl = "/images/products/" + uniqueFileName;
            }

            var product = new Product
            {
                Name = model.Name,
                CategoryId = model.CategoryId,
                ShortDescription = model.ShortDescription,
                LongDescription = model.LongDescription,
                Price = model.Price,
                DiscountPercentage = model.DiscountPercentage,
                StockQuantity = model.StockQuantity,
                ImageUrl = imageUrl,
                IsFeatured = model.IsFeatured,
                IsActive = model.IsActive,
                WeightDescription = model.WeightDescription,
                CuringMethod = model.CuringMethod,
                OriginFarm = model.OriginFarm,
                Rating = 5.0,
                ReviewCount = 0
            };

            await _productService.CreateProductAsync(product);
            TempData["SuccessMessage"] = $"Product '{product.Name}' created successfully!";

            return RedirectToAction(nameof(Products));
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            var categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();

            var model = new AdminProductEditViewModel
            {
                Id = product.Id,
                Name = product.Name,
                CategoryId = product.CategoryId,
                ShortDescription = product.ShortDescription,
                LongDescription = product.LongDescription,
                Price = product.Price,
                DiscountPercentage = product.DiscountPercentage,
                StockQuantity = product.StockQuantity,
                ImageUrl = product.ImageUrl,
                IsFeatured = product.IsFeatured,
                IsActive = product.IsActive,
                WeightDescription = product.WeightDescription,
                CuringMethod = product.CuringMethod,
                OriginFarm = product.OriginFarm,
                AvailableCategories = categories
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(AdminProductEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableCategories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
                return View(model);
            }

            var product = await _productService.GetProductByIdAsync(model.Id);
            if (product == null) return NotFound();

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using var fileStream = new FileStream(filePath, FileMode.Create);
                await model.ImageFile.CopyToAsync(fileStream);
                product.ImageUrl = "/images/products/" + uniqueFileName;
            }
            else if (!string.IsNullOrEmpty(model.ImageUrl))
            {
                product.ImageUrl = model.ImageUrl;
            }

            product.Name = model.Name;
            product.CategoryId = model.CategoryId;
            product.ShortDescription = model.ShortDescription;
            product.LongDescription = model.LongDescription;
            product.Price = model.Price;
            product.DiscountPercentage = model.DiscountPercentage;
            product.StockQuantity = model.StockQuantity;
            product.IsFeatured = model.IsFeatured;
            product.IsActive = model.IsActive;
            product.WeightDescription = model.WeightDescription;
            product.CuringMethod = model.CuringMethod;
            product.OriginFarm = model.OriginFarm;

            await _productService.UpdateProductAsync(product);
            TempData["SuccessMessage"] = $"Product '{product.Name}' updated successfully!";

            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            await _productService.DeleteProductAsync(id);
            TempData["SuccessMessage"] = "Product deleted successfully!";
            return RedirectToAction(nameof(Products));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkProductAction(string actionType, int[] selectedIds, decimal? priceAdjustPercent, int? newCategoryId)
        {
            if (selectedIds == null || selectedIds.Length == 0)
            {
                TempData["ErrorMessage"] = "No products selected for bulk action.";
                return RedirectToAction(nameof(Products));
            }

            var products = await _db.Products.Where(p => selectedIds.Contains(p.Id)).ToListAsync();

            switch (actionType)
            {
                case "delete":
                    _db.Products.RemoveRange(products);
                    TempData["SuccessMessage"] = $"Deleted {products.Count} products.";
                    break;

                case "activate":
                    foreach (var p in products) p.IsActive = true;
                    TempData["SuccessMessage"] = $"Activated {products.Count} products.";
                    break;

                case "deactivate":
                    foreach (var p in products) p.IsActive = false;
                    TempData["SuccessMessage"] = $"Deactivated {products.Count} products.";
                    break;

                case "changeCategory":
                    if (newCategoryId.HasValue && newCategoryId.Value > 0)
                    {
                        foreach (var p in products) p.CategoryId = newCategoryId.Value;
                        TempData["SuccessMessage"] = $"Updated category for {products.Count} products.";
                    }
                    break;

                case "adjustPrice":
                    if (priceAdjustPercent.HasValue && priceAdjustPercent.Value != 0)
                    {
                        var multiplier = 1 + (priceAdjustPercent.Value / 100m);
                        foreach (var p in products)
                        {
                            p.Price = Math.Round(p.Price * multiplier, 2);
                        }
                        TempData["SuccessMessage"] = $"Adjusted prices for {products.Count} products by {priceAdjustPercent.Value}%";
                    }
                    break;
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Products));
        }

        #endregion

        #region 3. Order Management

        public async Task<IActionResult> Orders(string? tab = "all", string? search = null, PaymentStatus? paymentFilter = null, OrderStatus? statusFilter = null)
        {
            var query = _db.Orders.Include(o => o.Items).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower().Trim();
                query = query.Where(o => o.OrderNumber.ToLower().Contains(term) ||
                                         o.CustomerName.ToLower().Contains(term) ||
                                         o.CustomerEmail.ToLower().Contains(term));
            }

            if (tab == "pending")
            {
                query = query.Where(o => o.DeliveryStatus == OrderStatus.Pending || o.DeliveryStatus == OrderStatus.Processing);
            }
            else if (tab == "completed")
            {
                query = query.Where(o => o.DeliveryStatus == OrderStatus.Delivered);
            }
            else if (tab == "cancelled")
            {
                query = query.Where(o => o.DeliveryStatus == OrderStatus.Cancelled);
            }

            if (paymentFilter.HasValue)
            {
                query = query.Where(o => o.PaymentStatus == paymentFilter.Value);
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(o => o.DeliveryStatus == statusFilter.Value);
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            var viewModel = new AdminOrderListViewModel
            {
                Orders = orders,
                ActiveTab = tab,
                Search = search,
                PaymentFilter = paymentFilter,
                StatusFilter = statusFilter
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();

            return PartialView("_AdminOrderDetailModal", order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus status)
        {
            await _orderService.UpdateOrderStatusAsync(orderId, status);
            TempData["SuccessMessage"] = $"Order #{orderId} status updated to {status}.";
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderPaymentStatus(int orderId, PaymentStatus status)
        {
            await _orderService.UpdatePaymentStatusAsync(orderId, status);
            TempData["SuccessMessage"] = $"Order #{orderId} payment status updated to {status}.";
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(int orderId)
        {
            await _orderService.DeleteOrderAsync(orderId);
            TempData["SuccessMessage"] = $"Order #{orderId} deleted.";
            return RedirectToAction(nameof(Orders));
        }

        #endregion

        #region 4. Customer Management

        public async Task<IActionResult> Customers(string? search)
        {
            var query = _userManager.Users.Include(u => u.Orders).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower().Trim();
                query = query.Where(u => (u.FullName != null && u.FullName.ToLower().Contains(term)) ||
                                         (u.Email != null && u.Email.ToLower().Contains(term)) ||
                                         (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
            }

            var users = await query.ToListAsync();

            var customerItems = users.Select(u => new AdminCustomerItemViewModel
            {
                Id = u.Id,
                FullName = u.FullName ?? "Customer",
                Email = u.Email ?? "",
                PhoneNumber = u.PhoneNumber ?? "",
                AvatarUrl = u.AvatarUrl ?? "/images/avatars/default.png",
                JoinDate = u.CreatedAt,
                TotalOrders = u.Orders.Count,
                LifetimeSpend = u.Orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).Sum(o => o.TotalAmount),
                LastOrderDate = u.Orders.OrderByDescending(o => o.CreatedAt).FirstOrDefault()?.CreatedAt,
                IsBlocked = u.IsBlocked,
                Notes = u.AdminNotes
            }).OrderByDescending(c => c.LifetimeSpend).ToList();

            var viewModel = new AdminCustomerListViewModel
            {
                Customers = customerItems,
                Search = search
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CustomerDetails(string id)
        {
            var customer = await _userManager.Users
                .Include(u => u.Orders)
                .ThenInclude(o => o.Items)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (customer == null) return NotFound();

            var viewModel = new AdminCustomerDetailViewModel
            {
                Customer = customer,
                Orders = customer.Orders.OrderByDescending(o => o.CreatedAt).ToList(),
                LifetimeSpend = customer.Orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).Sum(o => o.TotalAmount),
                TotalOrdersCount = customer.Orders.Count
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCustomerNotes(string customerId, string? notes)
        {
            var customer = await _userManager.FindByIdAsync(customerId);
            if (customer != null)
            {
                customer.AdminNotes = notes;
                await _userManager.UpdateAsync(customer);
                TempData["SuccessMessage"] = "Customer notes updated.";
            }

            return RedirectToAction(nameof(CustomerDetails), new { id = customerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlockCustomer(string customerId)
        {
            var customer = await _userManager.FindByIdAsync(customerId);
            if (customer != null)
            {
                customer.IsBlocked = !customer.IsBlocked;
                await _userManager.UpdateAsync(customer);
                TempData["SuccessMessage"] = customer.IsBlocked ? "Customer blocked." : "Customer unblocked.";
            }

            return RedirectToAction(nameof(Customers));
        }

        #endregion

        #region 5. Analytics & Reports

        public async Task<IActionResult> Analytics(DateTime? startDate, DateTime? endDate)
        {
            var summary = await _analyticsService.GetDashboardSummaryAsync();
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var viewModel = new AdminAnalyticsViewModel
            {
                Summary = summary,
                StartDate = start,
                EndDate = end
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ExportRevenueCsv(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var csvBytes = await _analyticsService.GenerateRevenueReportCsvAsync(start, end);
            var fileName = $"TJPork_Revenue_Report_{start:yyyyMMdd}_to_{end:yyyyMMdd}.csv";

            return File(csvBytes, "text/csv", fileName);
        }

        #endregion

        #region 6. Settings

        public async Task<IActionResult> Settings()
        {
            var settings = await _db.StoreSettings.ToListAsync();

            var model = new AdminSettingsViewModel
            {
                StoreName = settings.FirstOrDefault(s => s.Key == "StoreName")?.Value ?? "T&JPork Artisanal Meats",
                ContactEmail = settings.FirstOrDefault(s => s.Key == "ContactEmail")?.Value ?? "orders@tjpork.com",
                ContactPhone = settings.FirstOrDefault(s => s.Key == "ContactPhone")?.Value ?? "+1 (555) 835-7675",
                StandardDeliveryFee = decimal.TryParse(settings.FirstOrDefault(s => s.Key == "StandardDeliveryFee")?.Value, out var df) ? df : 5.99m,
                FreeDeliveryThreshold = decimal.TryParse(settings.FirstOrDefault(s => s.Key == "FreeDeliveryThreshold")?.Value, out var ft) ? ft : 50.00m,
                TaxRatePercentage = decimal.TryParse(settings.FirstOrDefault(s => s.Key == "TaxRatePercentage")?.Value, out var tr) ? tr : 5.0m,
                AdminNotifyEmails = settings.FirstOrDefault(s => s.Key == "AdminNotifyEmails")?.Value ?? "tinashe@tjfork.com,jeffery@tjfork.com"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(AdminSettingsViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            async Task UpsertSetting(string key, string value, string desc, string group)
            {
                var setting = await _db.StoreSettings.FirstOrDefaultAsync(s => s.Key == key);
                if (setting == null)
                {
                    _db.StoreSettings.Add(new StoreSetting { Key = key, Value = value, Description = desc, Group = group });
                }
                else
                {
                    setting.Value = value;
                }
            }

            await UpsertSetting("StoreName", model.StoreName, "Public store name", "General");
            await UpsertSetting("ContactEmail", model.ContactEmail, "Customer support email", "General");
            await UpsertSetting("ContactPhone", model.ContactPhone, "Customer support phone", "General");
            await UpsertSetting("StandardDeliveryFee", model.StandardDeliveryFee.ToString("F2"), "Standard flat delivery fee", "Delivery");
            await UpsertSetting("FreeDeliveryThreshold", model.FreeDeliveryThreshold.ToString("F2"), "Cart value for free delivery", "Delivery");
            await UpsertSetting("TaxRatePercentage", model.TaxRatePercentage.ToString("F1"), "Artisanal food tax rate percentage", "Payment");
            await UpsertSetting("AdminNotifyEmails", model.AdminNotifyEmails, "Owner notification recipients", "Notifications");

            await _db.SaveChangesAsync();

            model.SuccessMessage = "Store configuration and delivery settings updated successfully!";
            return View(model);
        }

        #endregion
    }
}
