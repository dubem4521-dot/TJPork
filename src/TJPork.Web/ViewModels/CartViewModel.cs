using System;
using System.ComponentModel.DataAnnotations;
using TJPork.Core.Enums;
using TJPork.Core.Models;

namespace TJPork.Web.ViewModels
{
    public class CartViewModel
    {
        public Cart Cart { get; set; } = new Cart();
        public string? CouponError { get; set; }
        public string? CouponSuccess { get; set; }
        public decimal FreeShippingThreshold { get; set; } = 500.00m;
        public decimal AmountNeededForFreeShipping => Math.Max(0, FreeShippingThreshold - Cart.Subtotal);
        public int FreeShippingProgressPercent => (int)Math.Min(100, Math.Round((Cart.Subtotal / FreeShippingThreshold) * 100));
    }

    public class CheckoutViewModel
    {
        public Cart Cart { get; set; } = new Cart();

        [Required(ErrorMessage = "Please enter your full name")]
        [Display(Name = "Full Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email Address")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your contact phone number")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your delivery street address")]
        [Display(Name = "Street Address")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your city")]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your postal / zip code")]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a preferred delivery date")]
        [Display(Name = "Delivery Date")]
        [DataType(DataType.Date)]
        public DateTime DeliveryDate { get; set; } = DateTime.UtcNow.AddDays(2);

        [Display(Name = "Delivery Time Slot")]
        public DeliverySlot DeliverySlot { get; set; } = DeliverySlot.Morning;

        [Display(Name = "Delivery Instructions / Gate Code / Complex Unit")]
        public string? SpecialInstructions { get; set; }

        [Required(ErrorMessage = "Please choose a payment method")]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

        // Card fields for simulated card processing
        [Display(Name = "Cardholder Name")]
        public string? CardHolderName { get; set; }

        [Display(Name = "Card Number")]
        public string? CardNumber { get; set; }

        [Display(Name = "Expiration (MM/YY)")]
        public string? CardExpiry { get; set; }

        [Display(Name = "CVC / CVV")]
        public string? CardCvc { get; set; }
    }

    public class OrderConfirmationViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public DateTime DeliveryDate { get; set; }
        public DeliverySlot DeliverySlot { get; set; }
        public decimal TotalAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public OrderStatus DeliveryStatus { get; set; }
        public System.Collections.Generic.List<Core.Entities.OrderItem> Items { get; set; } = new System.Collections.Generic.List<Core.Entities.OrderItem>();
    }
}
