using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TJPork.Core.Entities;
using TJPork.Core.Enums;
using TJPork.Infrastructure.Data;
using TJPork.Infrastructure.Identity;
using TJPork.Web.ViewModels;

namespace TJPork.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TJPorkDbContext _db;

        public ProfileController(UserManager<ApplicationUser> userManager, TJPorkDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var orders = await _db.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.UserId == user.Id || o.CustomerEmail == user.Email)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var activeOrders = orders.Where(o => o.DeliveryStatus != OrderStatus.Delivered && o.DeliveryStatus != OrderStatus.Cancelled).ToList();
            var orderHistory = orders.Where(o => o.DeliveryStatus == OrderStatus.Delivered || o.DeliveryStatus == OrderStatus.Cancelled).ToList();

            var addresses = await _db.Addresses
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            var notifications = await _db.Notifications
                .Where(n => n.UserId == user.Id || n.UserId == null)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();

            var totalSpent = orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).Sum(o => o.TotalAmount);

            var viewModel = new ProfileViewModel
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                DeliveryAddress = user.DeliveryAddress ?? "",
                City = user.City ?? "",
                PostalCode = user.PostalCode ?? "",
                AvatarUrl = user.AvatarUrl ?? "/images/avatars/default.png",
                MemberSince = user.CreatedAt,
                TotalOrdersCount = orders.Count,
                TotalSpent = totalSpent,
                ActiveOrders = activeOrders,
                OrderHistory = orderHistory,
                SavedAddresses = addresses,
                Notifications = notifications,
                NewAddress = new AddressFormModel { RecipientName = user.FullName, Phone = user.PhoneNumber ?? "" }
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber, string deliveryAddress, string city, string postalCode)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            user.DeliveryAddress = deliveryAddress;
            user.City = city;
            user.PostalCode = postalCode;

            await _userManager.UpdateAsync(user);
            TempData["SuccessMessage"] = "Profile details updated successfully!";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(AddressFormModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all address fields.";
                return RedirectToAction(nameof(Index));
            }

            if (model.IsDefault)
            {
                var existingDefaults = await _db.Addresses.Where(a => a.UserId == user.Id && a.IsDefault).ToListAsync();
                foreach (var addr in existingDefaults)
                {
                    addr.IsDefault = false;
                }
            }

            var newAddress = new Address
            {
                UserId = user.Id,
                Label = model.Label,
                RecipientName = model.RecipientName,
                Phone = model.Phone,
                StreetAddress = model.StreetAddress,
                City = model.City,
                PostalCode = model.PostalCode,
                IsDefault = model.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            _db.Addresses.Add(newAddress);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "New delivery address saved!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
            if (address != null)
            {
                _db.Addresses.Remove(address);
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Address removed.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var notification = await _db.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _db.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            string? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                userId = user?.Id;
            }

            var notifications = await _db.Notifications
                .Where(n => n.UserId == userId || n.UserId == null)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new
                {
                    id = n.Id,
                    title = n.Title,
                    message = n.Message,
                    linkUrl = n.LinkUrl,
                    iconClass = n.IconClass,
                    isRead = n.IsRead,
                    timeAgo = GetTimeAgo(n.CreatedAt)
                })
                .ToListAsync();

            var unreadCount = notifications.Count(n => !n.isRead);

            return Json(new { notifications, unreadCount });
        }

        private static string GetTimeAgo(DateTime dt)
        {
            var span = DateTime.UtcNow - dt;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
            return $"{(int)span.TotalDays}d ago";
        }
    }
}
