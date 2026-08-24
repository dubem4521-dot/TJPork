using System;
using System.Collections.Generic;
using System.Linq;

namespace TJPork.Core.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSlug { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string WeightDescription { get; set; } = string.Empty;

        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public decimal Subtotal => Items.Sum(i => i.TotalPrice);

        public decimal DeliveryFee => (Subtotal >= 500m || Items.Count == 0) ? 0m : 65.00m;

        public decimal TaxRate => 0.15m; // 15% South African VAT

        public decimal TaxAmount => Math.Round(Subtotal * TaxRate, 2);

        public decimal DiscountAmount { get; set; } = 0m;

        public string? CouponCode { get; set; }

        public decimal Total => Math.Max(0m, Subtotal + DeliveryFee + TaxAmount - DiscountAmount);

        public int TotalItemCount => Items.Sum(i => i.Quantity);

        public void AddItem(int productId, string productName, string productSlug, decimal unitPrice, int quantity, string imageUrl, string weightDesc)
        {
            var existing = Items.FirstOrDefault(i => i.ProductId == productId);
            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                Items.Add(new CartItem
                {
                    ProductId = productId,
                    ProductName = productName,
                    ProductSlug = productSlug,
                    UnitPrice = unitPrice,
                    Quantity = quantity,
                    ImageUrl = imageUrl,
                    WeightDescription = weightDesc
                });
            }
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    Items.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
            }
        }

        public void RemoveItem(int productId)
        {
            Items.RemoveAll(i => i.ProductId == productId);
        }

        public void Clear()
        {
            Items.Clear();
            DiscountAmount = 0m;
            CouponCode = null;
        }
    }
}
