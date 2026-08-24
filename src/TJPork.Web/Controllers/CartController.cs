using Microsoft.AspNetCore.Mvc;
using TJPork.Core.Interfaces;
using TJPork.Web.ViewModels;

namespace TJPork.Web.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            var viewModel = new CartViewModel
            {
                Cart = cart
            };
            return View(viewModel);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            _cartService.AddToCart(productId, quantity);
            var cart = _cartService.GetCart();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new
                {
                    success = true,
                    message = "Item added to your artisanal pork cart!",
                    itemCount = cart.TotalItemCount,
                    subtotal = cart.Subtotal,
                    total = cart.Total
                });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            _cartService.UpdateQuantity(productId, quantity);
            var cart = _cartService.GetCart();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new
                {
                    success = true,
                    itemCount = cart.TotalItemCount,
                    subtotal = cart.Subtotal,
                    deliveryFee = cart.DeliveryFee,
                    taxAmount = cart.TaxAmount,
                    discountAmount = cart.DiscountAmount,
                    total = cart.Total
                });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            _cartService.RemoveFromCart(productId);
            var cart = _cartService.GetCart();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new
                {
                    success = true,
                    message = "Item removed from cart",
                    itemCount = cart.TotalItemCount,
                    subtotal = cart.Subtotal,
                    total = cart.Total
                });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult ClearCart()
        {
            _cartService.ClearCart();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart = _cartService.GetCart();
            return Json(new { count = cart.TotalItemCount, total = cart.Total });
        }

        [HttpGet]
        public IActionResult GetCartDrawer()
        {
            var cart = _cartService.GetCart();
            return PartialView("_CartDrawerContent", cart);
        }

        [HttpPost]
        public IActionResult ApplyCoupon(string couponCode)
        {
            var success = _cartService.ApplyCoupon(couponCode);
            var cart = _cartService.GetCart();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new
                {
                    success,
                    message = success ? $"Coupon {couponCode} applied!" : "Invalid or expired promo code.",
                    discount = cart.DiscountAmount,
                    total = cart.Total
                });
            }

            if (!success)
            {
                TempData["CouponError"] = "Invalid or ineligible coupon code. Try PORK10 or FREESHIP.";
            }
            else
            {
                TempData["CouponSuccess"] = $"Coupon applied successfully!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
