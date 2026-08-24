using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TJPork.Core.Entities;
using TJPork.Core.Enums;
using TJPork.Core.Interfaces;
using TJPork.Infrastructure.Identity;
using TJPork.Web.ViewModels;

namespace TJPork.Web.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(ICartService cartService, IOrderService orderService, UserManager<ApplicationUser> userManager)
        {
            _cartService = cartService;
            _orderService = orderService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = _cartService.GetCart();
            if (cart.Items.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutViewModel
            {
                Cart = cart,
                DeliveryDate = DateTime.UtcNow.AddDays(2),
                DeliverySlot = DeliverySlot.Morning
            };

            // Pre-fill user information if logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    model.CustomerName = user.FullName ?? "";
                    model.CustomerEmail = user.Email ?? "";
                    model.CustomerPhone = user.PhoneNumber ?? "";
                    model.ShippingAddress = user.DeliveryAddress ?? "";
                    model.City = user.City ?? "";
                    model.PostalCode = user.PostalCode ?? "";
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel model)
        {
            var cart = _cartService.GetCart();
            if (cart.Items.Count == 0)
            {
                ModelState.AddModelError("", "Your shopping cart is currently empty.");
                return RedirectToAction("Index", "Cart");
            }

            model.Cart = cart;

            if (model.PaymentMethod == PaymentMethod.CreditCard)
            {
                if (string.IsNullOrWhiteSpace(model.CardNumber) || model.CardNumber.Replace(" ", "").Length < 15)
                {
                    ModelState.AddModelError("CardNumber", "Please enter a valid 16-digit card number.");
                }
                if (string.IsNullOrWhiteSpace(model.CardExpiry))
                {
                    ModelState.AddModelError("CardExpiry", "Please enter card expiration date (MM/YY).");
                }
                if (string.IsNullOrWhiteSpace(model.CardCvc) || model.CardCvc.Length < 3)
                {
                    ModelState.AddModelError("CardCvc", "Please enter 3 or 4-digit CVC code.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                userId = user?.Id;
            }

            var order = new Order
            {
                UserId = userId,
                CustomerName = model.CustomerName,
                CustomerEmail = model.CustomerEmail,
                CustomerPhone = model.CustomerPhone,
                ShippingAddress = model.ShippingAddress,
                City = model.City,
                PostalCode = model.PostalCode,
                DeliveryDate = model.DeliveryDate,
                DeliverySlot = model.DeliverySlot,
                SpecialInstructions = model.SpecialInstructions,
                PaymentMethod = model.PaymentMethod
            };

            var createdOrder = await _orderService.CreateOrderAsync(order, cart);

            // Clear session cart
            _cartService.ClearCart();

            return RedirectToAction(nameof(Confirmation), new { orderNumber = createdOrder.OrderNumber });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(string orderNumber)
        {
            if (string.IsNullOrEmpty(orderNumber)) return RedirectToAction("Index", "Home");

            var order = await _orderService.GetOrderByNumberAsync(orderNumber);
            if (order == null) return NotFound();

            var viewModel = new OrderConfirmationViewModel
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                CustomerPhone = order.CustomerPhone,
                ShippingAddress = order.ShippingAddress,
                City = order.City,
                PostalCode = order.PostalCode,
                DeliveryDate = order.DeliveryDate,
                DeliverySlot = order.DeliverySlot,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                DeliveryStatus = order.DeliveryStatus,
                Items = order.Items.ToList()
            };

            return View(viewModel);
        }
    }
}
