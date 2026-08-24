using System.Collections.Generic;
using System.Threading.Tasks;
using TJPork.Core.Entities;
using TJPork.Core.Enums;
using TJPork.Core.Models;

namespace TJPork.Core.Interfaces
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(Order order, Cart cart);
        Task<Order?> GetOrderByIdAsync(int id);
        Task<Order?> GetOrderByNumberAsync(string orderNumber);
        Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId);
        Task<IEnumerable<Order>> GetAllOrdersAsync(OrderStatus? status = null, PaymentStatus? paymentStatus = null);
        Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus status);
        Task<bool> UpdatePaymentStatusAsync(int orderId, PaymentStatus status);
        Task<bool> DeleteOrderAsync(int orderId);
        string GenerateOrderNumber();
    }
}
