using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TJPork.Core.Entities;
using TJPork.Core.Interfaces;
using TJPork.Infrastructure.Data;
using TJPork.Infrastructure.Identity;
using TJPork.Web.ViewModels;

namespace TJPork.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly TJPorkDbContext _db;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager, 
            TJPorkDbContext db,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Profile");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email address or password.");
                return View(model);
            }

            if (user.IsBlocked)
            {
                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact support.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }

                return RedirectToAction("Index", "Profile");
            }

            ModelState.AddModelError(string.Empty, "Invalid email address or password.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Profile");
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                DeliveryAddress = model.DeliveryAddress,
                City = model.City,
                PostalCode = model.PostalCode,
                CreatedAt = System.DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");

                // Seed default address
                _db.Addresses.Add(new Address
                {
                    UserId = user.Id,
                    Label = "Primary Home",
                    RecipientName = model.FullName,
                    Phone = model.PhoneNumber,
                    StreetAddress = model.DeliveryAddress,
                    City = model.City,
                    PostalCode = model.PostalCode,
                    IsDefault = true
                });

                // Welcome notification
                _db.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Title = "Welcome to T&JPork!",
                    Message = "Thank you for joining our artisanal community. Enjoy farm-fresh heritage cuts delivered directly to your doorstep.",
                    LinkUrl = "/Shop",
                    IconClass = "fa-solid fa-gift",
                    CreatedAt = System.DateTime.UtcNow
                });

                await _db.SaveChangesAsync();

                // Send Email Sign-Up Notifications (Welcome to customer & alert to admin)
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email!, user.FullName);
                    await _emailService.SendAdminNewUserAlertAsync(user.Email!, user.FullName, user.PhoneNumber ?? "", user.DeliveryAddress ?? "", user.City ?? "");
                }
                catch
                {
                    // Suppress background email errors during registration
                }

                await _signInManager.SignInAsync(user, isPersistent: true);
                return RedirectToAction("Index", "Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Profile");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);
                if (!string.IsNullOrEmpty(resetLink))
                {
                    try
                    {
                        await _emailService.SendPasswordResetEmailAsync(user.Email!, user.FullName, resetLink);
                    }
                    catch
                    {
                        // Logged internally
                    }
                }
            }

            ViewBag.Submitted = true;
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return RedirectToAction(nameof(Login));
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal user existence
                TempData["SuccessMessage"] = "Your password has been reset successfully. Please sign in.";
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Your password has been reset successfully. Please sign in with your new password.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
