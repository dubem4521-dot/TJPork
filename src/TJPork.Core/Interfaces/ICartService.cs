using TJPork.Core.Models;

namespace TJPork.Core.Interfaces
{
    public interface ICartService
    {
        Cart GetCart();
        void AddToCart(int productId, int quantity = 1);
        void UpdateQuantity(int productId, int quantity);
        void RemoveFromCart(int productId);
        void ClearCart();
        bool ApplyCoupon(string couponCode);
    }
}
