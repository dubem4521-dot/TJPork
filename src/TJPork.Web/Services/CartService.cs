using System;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using TJPork.Core.Entities;
using TJPork.Core.Interfaces;
using TJPork.Core.Models;

namespace TJPork.Web.Services
{
    public class CartService : ICartService
    {
        private const string CartSessionKey = "TJPork_ShoppingCart_Session";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IProductService _productService;

        public CartService(IHttpContextAccessor httpContextAccessor, IProductService productService)
        {
            _httpContextAccessor = httpContextAccessor;
            _productService = productService;
        }

        private ISession Session => _httpContextAccessor.HttpContext?.Session 
            ?? throw new System.InvalidOperationException("Session is unavailable.");

        public Cart GetCart()
        {
            var cartJson = Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                var newCart = new Cart();
                SaveCart(newCart);
                return newCart;
            }

            try
            {
                return JsonSerializer.Deserialize<Cart>(cartJson) ?? new Cart();
            }
            catch
            {
                var fallbackCart = new Cart();
                SaveCart(fallbackCart);
                return fallbackCart;
            }
        }

        private void SaveCart(Cart cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            Session.SetString(CartSessionKey, cartJson);
        }

        public void AddToCart(int productId, int quantity = 1)
        {
            var product = _productService.GetProductByIdAsync(productId).GetAwaiter().GetResult();
            if (product == null || !product.IsActive) return;

            var cart = GetCart();
            cart.AddItem(
                product.Id,
                product.Name,
                product.Slug,
                product.FinalPrice,
                quantity,
                product.ImageUrl,
                product.WeightDescription
            );

            SaveCart(cart);
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            var cart = GetCart();
            cart.UpdateQuantity(productId, quantity);
            SaveCart(cart);
        }

        public void RemoveFromCart(int productId)
        {
            var cart = GetCart();
            cart.RemoveItem(productId);
            SaveCart(cart);
        }

        public void ClearCart()
        {
            var cart = GetCart();
            cart.Clear();
            SaveCart(cart);
        }

        public bool ApplyCoupon(string couponCode)
        {
            var cart = GetCart();
            var code = couponCode?.Trim().ToUpperInvariant();

            if (code == "PORK10" && cart.Subtotal > 200m)
            {
                cart.CouponCode = "PORK10 (10% Off)";
                cart.DiscountAmount = Math.Round(cart.Subtotal * 0.10m, 2);
                SaveCart(cart);
                return true;
            }
            else if (code == "FREESHIP" && cart.Subtotal > 150m)
            {
                cart.CouponCode = "FREESHIP (Free Delivery)";
                cart.DiscountAmount = cart.DeliveryFee;
                SaveCart(cart);
                return true;
            }
            else if (code == "TJPORK50" && cart.Subtotal > 300m)
            {
                cart.CouponCode = "TJPORK50 (R50 Off)";
                cart.DiscountAmount = 50.00m;
                SaveCart(cart);
                return true;
            }

            return false;
        }
    }
}
