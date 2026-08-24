namespace TJPork.Core.Enums
{
    public enum OrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled
    }

    public enum PaymentStatus
    {
        Pending,
        Paid,
        Failed,
        Refunded
    }

    public enum PaymentMethod
    {
        CashOnDelivery,
        CreditCard
    }

    public enum DeliverySlot
    {
        Morning,    // 9:00 AM - 12:00 PM
        Afternoon,  // 1:00 PM - 5:00 PM
        Evening     // 6:00 PM - 9:00 PM
    }
}
