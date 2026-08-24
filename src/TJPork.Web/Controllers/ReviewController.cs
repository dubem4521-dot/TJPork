using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TJPork.Core.Entities;
using TJPork.Infrastructure.Data;
using TJPork.Infrastructure.Identity;
using TJPork.Web.ViewModels;

namespace TJPork.Web.Controllers
{
    public class ReviewController : Controller
    {
        private readonly TJPorkDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(TJPorkDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(ReviewFormModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all required fields to submit your review.";
                return Redirect(returnUrl ?? "/");
            }

            var product = await _db.Products.FindAsync(model.ProductId);
            if (product == null)
            {
                return NotFound();
            }

            string? userId = null;
            string customerName = model.CustomerName;
            string? customerEmail = model.CustomerEmail;

            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    userId = user.Id;
                    customerName = user.FullName;
                    customerEmail = user.Email;
                }
            }

            if (string.IsNullOrWhiteSpace(customerName))
            {
                customerName = "Artisanal Pork Enthusiast";
            }

            var review = new Review
            {
                ProductId = model.ProductId,
                UserId = userId,
                CustomerName = customerName,
                CustomerEmail = customerEmail,
                Rating = Math.Clamp(model.Rating, 1, 5),
                Title = model.Title,
                Comment = model.Comment,
                IsVerifiedBuyer = true,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            // Recalculate average rating & review count for product
            var reviews = await _db.Reviews.Where(r => r.ProductId == model.ProductId && r.IsApproved).ToListAsync();
            product.ReviewCount = reviews.Count;
            product.Rating = Math.Round(reviews.Average(r => r.Rating), 1);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you! Your review has been published.";
            return Redirect(returnUrl ?? "/");
        }
    }
}
