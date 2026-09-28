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
                if (!IsValidImageFile(model.ImageFile, out var error))
                {
                    ModelState.AddModelError("ImageFile", error);
                    model.AvailableCategories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
                    return View(model);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
                Directory.CreateDirectory(uploadsFolder);
                var safeExt = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                var uniqueFileName = Guid.NewGuid().ToString() + safeExt;
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
                if (!IsValidImageFile(model.ImageFile, out var error))
                {
                    ModelState.AddModelError("ImageFile", error);
                    model.AvailableCategories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
                    return View(model);
                }

                var uploadsFolder = Path.Combine(_env.WebRootPath, "images", "products");
                Directory.CreateDirectory(uploadsFolder);
                var safeExt = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                var uniqueFileName = Guid.NewGuid().ToString() + safeExt;
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

                case "feature":
                    foreach (var p in products) p.IsFeatured = true;
                    TempData["SuccessMessage"] = $"Set {products.Count} products as Featured on Homepage.";
                    break;

                case "unfeature":
                    foreach (var p in products) p.IsFeatured = false;
                    TempData["SuccessMessage"] = $"Removed {products.Count} products from Homepage Featured.";
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
                ContactPhone = settings.FirstOrDefault(s => s.Key == "ContactPhone")?.Value ?? "+27 (0)21 835 7675",
                StandardDeliveryFee = decimal.TryParse(settings.FirstOrDefault(s => s.Key == "StandardDeliveryFee")?.Value, out var df) ? df : 65.00m,
                FreeDeliveryThreshold = decimal.TryParse(settings.FirstOrDefault(s => s.Key == "FreeDeliveryThreshold")?.Value, out var ft) ? ft : 500.00m,
                TaxRatePercentage = decimal.TryParse(settings.FirstOrDefault(s => s.Key == "TaxRatePercentage")?.Value, out var tr) ? tr : 15.0m,
                AdminNotifyEmails = settings.FirstOrDefault(s => s.Key == "AdminNotifyEmails")?.Value ?? "tinashe@tjpork.com"
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

        #region 6. Admin Team Management

        [HttpGet]
        public async Task<IActionResult> Admins()
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var currentUserId = _userManager.GetUserId(User);

            var model = new AdminUserListViewModel
            {
                CurrentAdminId = currentUserId ?? string.Empty,
                Admins = adminUsers.Select(u => new AdminUserItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName ?? u.UserName ?? "Admin",
                    Email = u.Email ?? "",
                    PhoneNumber = u.PhoneNumber ?? "",
                    AvatarUrl = string.IsNullOrEmpty(u.AvatarUrl) ? "/images/avatars/default.png" : u.AvatarUrl,
                    CreatedAt = u.CreatedAt,
                    IsCurrentAdmin = u.Id == currentUserId
                }).OrderBy(a => a.FullName).ToList()
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateAdmin()
        {
            return View(new CreateAdminViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "A user with this email address already exists.");
                return View(model);
            }

            var newAdmin = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(newAdmin, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                {
                    ModelState.AddModelError("", err.Description);
                }
                return View(model);
            }

            await _userManager.AddToRoleAsync(newAdmin, "Admin");
            TempData["SuccessMessage"] = $"New administrator '{model.FullName}' created successfully!";
            return RedirectToAction(nameof(Admins));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAdmin(string id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot remove your own admin account.";
                return RedirectToAction(nameof(Admins));
            }

            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            if (adminUsers.Count <= 1)
            {
                TempData["ErrorMessage"] = "Cannot remove the only remaining administrator.";
                return RedirectToAction(nameof(Admins));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Admins));
            }

            var roleResult = await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (roleResult.Succeeded)
            {
                TempData["SuccessMessage"] = $"Administrator privileges removed for '{user.FullName}'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update administrator role.";
            }

            return RedirectToAction(nameof(Admins));
        }

        #endregion

        #region 7. Admin Profile Management

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = new AdminProfileViewModel
            {
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                AvatarUrl = string.IsNullOrEmpty(user.AvatarUrl) ? "/images/avatars/default.png" : user.AvatarUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(AdminProfileViewModel model, Microsoft.AspNetCore.Http.IFormFile? avatarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.AvatarUrl = user.AvatarUrl;
                return View(model);
            }

            if (avatarFile != null && avatarFile.Length > 0)
            {
                if (!IsValidImageFile(avatarFile, out var error))
                {
                    ModelState.AddModelError("AvatarFile", error);
                    model.AvatarUrl = user.AvatarUrl;
                    return View(model);
                }

                var avatarsFolder = Path.Combine(_env.WebRootPath, "images", "avatars");
                Directory.CreateDirectory(avatarsFolder);
                var safeExt = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                var uniqueFileName = Guid.NewGuid().ToString("N") + safeExt;
                var filePath = Path.Combine(avatarsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(fileStream);
                }

                user.AvatarUrl = "/images/avatars/" + uniqueFileName;
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError("Email", "This email address is already in use.");
                    model.AvatarUrl = user.AvatarUrl;
                    return View(model);
                }

                user.Email = model.Email;
                user.UserName = model.Email;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                {
                    ModelState.AddModelError("", err.Description);
                }
                model.AvatarUrl = user.AvatarUrl;
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Profile details updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(AdminChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all password fields accurately.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var changeResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!changeResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join("; ", changeResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Profile));
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "Password changed successfully!";
            return RedirectToAction(nameof(Profile));
        }

        #endregion

        #region 8. About Us CMS

        [HttpGet]
        public async Task<IActionResult> AboutUs()
        {
            var settings = await _db.StoreSettings
                .Where(s => s.Group == "AboutUs")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            var model = new AboutUsEditViewModel();

            if (settings.TryGetValue("AboutUs.HeaderSuperTitle", out var superTitle)) model.HeaderSuperTitle = superTitle;
            if (settings.TryGetValue("AboutUs.HeaderTitle", out var title)) model.HeaderTitle = title;
            if (settings.TryGetValue("AboutUs.HeaderIntro", out var intro)) model.HeaderIntro = intro;

            if (settings.TryGetValue("AboutUs.Founder1Name", out var f1Name)) model.Founder1Name = f1Name;
            if (settings.TryGetValue("AboutUs.Founder1Role", out var f1Role)) model.Founder1Role = f1Role;
            if (settings.TryGetValue("AboutUs.Founder1Bio", out var f1Bio)) model.Founder1Bio = f1Bio;
            if (settings.TryGetValue("AboutUs.Founder1ImageUrl", out var f1Img)) model.Founder1ImageUrl = f1Img;
            if (settings.TryGetValue("AboutUs.Founder1Badge1", out var f1B1)) model.Founder1Badge1 = f1B1;
            if (settings.TryGetValue("AboutUs.Founder1Badge2", out var f1B2)) model.Founder1Badge2 = f1B2;

            if (settings.TryGetValue("AboutUs.Founder2Name", out var f2Name)) model.Founder2Name = f2Name;
            if (settings.TryGetValue("AboutUs.Founder2Role", out var f2Role)) model.Founder2Role = f2Role;
            if (settings.TryGetValue("AboutUs.Founder2Bio", out var f2Bio)) model.Founder2Bio = f2Bio;
            if (settings.TryGetValue("AboutUs.Founder2ImageUrl", out var f2Img)) model.Founder2ImageUrl = f2Img;
            if (settings.TryGetValue("AboutUs.Founder2Badge1", out var f2B1)) model.Founder2Badge1 = f2B1;
            if (settings.TryGetValue("AboutUs.Founder2Badge2", out var f2B2)) model.Founder2Badge2 = f2B2;

            if (settings.TryGetValue("AboutUs.Value1Title", out var v1T)) model.Value1Title = v1T;
            if (settings.TryGetValue("AboutUs.Value1Description", out var v1D)) model.Value1Description = v1D;
            if (settings.TryGetValue("AboutUs.Value2Title", out var v2T)) model.Value2Title = v2T;
            if (settings.TryGetValue("AboutUs.Value2Description", out var v2D)) model.Value2Description = v2D;
            if (settings.TryGetValue("AboutUs.Value3Title", out var v3T)) model.Value3Title = v3T;
            if (settings.TryGetValue("AboutUs.Value3Description", out var v3D)) model.Value3Description = v3D;

            if (settings.TryGetValue("AboutUs.MissionTitle", out var mT)) model.MissionTitle = mT;
            if (settings.TryGetValue("AboutUs.MissionQuote", out var mQ)) model.MissionQuote = mQ;
            if (settings.TryGetValue("AboutUs.MissionAttribution", out var mA)) model.MissionAttribution = mA;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AboutUs(AboutUsEditViewModel model, Microsoft.AspNetCore.Http.IFormFile? founder1ImageFile, Microsoft.AspNetCore.Http.IFormFile? founder2ImageFile)
        {
            var aboutFolder = Path.Combine(_env.WebRootPath, "images", "about");
            Directory.CreateDirectory(aboutFolder);

            // Handle Founder 1 Image Upload
            if (founder1ImageFile != null && founder1ImageFile.Length > 0)
            {
                if (IsValidImageFile(founder1ImageFile, out var error))
                {
                    var safeExt = Path.GetExtension(founder1ImageFile.FileName).ToLowerInvariant();
                    var fileName = "founder1_" + Guid.NewGuid().ToString("N")[..8] + safeExt;
                    var filePath = Path.Combine(aboutFolder, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await founder1ImageFile.CopyToAsync(stream);
                    model.Founder1ImageUrl = "/images/about/" + fileName;
                }
                else
                {
                    ModelState.AddModelError("Founder1ImageFile", error);
                }
            }

            // Handle Founder 2 Image Upload
            if (founder2ImageFile != null && founder2ImageFile.Length > 0)
            {
                if (IsValidImageFile(founder2ImageFile, out var error))
                {
                    var safeExt = Path.GetExtension(founder2ImageFile.FileName).ToLowerInvariant();
                    var fileName = "founder2_" + Guid.NewGuid().ToString("N")[..8] + safeExt;
                    var filePath = Path.Combine(aboutFolder, fileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await founder2ImageFile.CopyToAsync(stream);
                    model.Founder2ImageUrl = "/images/about/" + fileName;
                }
                else
                {
                    ModelState.AddModelError("Founder2ImageFile", error);
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var entries = new Dictionary<string, string>
            {
                { "AboutUs.HeaderSuperTitle", model.HeaderSuperTitle },
                { "AboutUs.HeaderTitle", model.HeaderTitle },
                { "AboutUs.HeaderIntro", model.HeaderIntro },

                { "AboutUs.Founder1Name", model.Founder1Name },
                { "AboutUs.Founder1Role", model.Founder1Role },
                { "AboutUs.Founder1Bio", model.Founder1Bio },
                { "AboutUs.Founder1ImageUrl", model.Founder1ImageUrl },
                { "AboutUs.Founder1Badge1", model.Founder1Badge1 },
                { "AboutUs.Founder1Badge2", model.Founder1Badge2 },

                { "AboutUs.Founder2Name", model.Founder2Name },
                { "AboutUs.Founder2Role", model.Founder2Role },
                { "AboutUs.Founder2Bio", model.Founder2Bio },
                { "AboutUs.Founder2ImageUrl", model.Founder2ImageUrl },
                { "AboutUs.Founder2Badge1", model.Founder2Badge1 },
                { "AboutUs.Founder2Badge2", model.Founder2Badge2 },

                { "AboutUs.Value1Title", model.Value1Title },
                { "AboutUs.Value1Description", model.Value1Description },
                { "AboutUs.Value2Title", model.Value2Title },
                { "AboutUs.Value2Description", model.Value2Description },
                { "AboutUs.Value3Title", model.Value3Title },
                { "AboutUs.Value3Description", model.Value3Description },

                { "AboutUs.MissionTitle", model.MissionTitle },
                { "AboutUs.MissionQuote", model.MissionQuote },
                { "AboutUs.MissionAttribution", model.MissionAttribution }
            };

            foreach (var kvp in entries)
            {
                var setting = await _db.StoreSettings.FirstOrDefaultAsync(s => s.Key == kvp.Key);
                if (setting == null)
                {
                    _db.StoreSettings.Add(new StoreSetting
                    {
                        Key = kvp.Key,
                        Value = kvp.Value ?? "",
                        Group = "AboutUs",
                        Description = kvp.Key
                    });
                }
                else
                {
                    setting.Value = kvp.Value ?? "";
                }
            }

            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "About Us content and photos updated successfully!";
            return RedirectToAction(nameof(AboutUs));
        }

        #endregion

        #region Helpers

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

        private static bool IsValidImageFile(Microsoft.AspNetCore.Http.IFormFile file, out string errorMessage)
        {
            errorMessage = string.Empty;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedImageExtensions.Contains(ext))
            {
                errorMessage = "Only image files (.jpg, .jpeg, .png, .webp) are permitted.";
                return false;
            }

            if (file.Length > MaxImageSizeBytes)
            {
                errorMessage = "Image file size exceeds the 5MB maximum limit.";
                return false;
            }

            return true;
        }

        #endregion
    }
}
