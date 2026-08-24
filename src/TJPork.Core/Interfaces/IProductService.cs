using System.Collections.Generic;
using System.Threading.Tasks;
using TJPork.Core.Entities;

namespace TJPork.Core.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync(bool activeOnly = true);
        Task<IEnumerable<Product>> GetFeaturedProductsAsync();
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(string categorySlug);
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product?> GetProductBySlugAsync(string slug);
        Task<IEnumerable<Product>> SearchProductsAsync(string? searchTerm, int? categoryId, decimal? minPrice, decimal? maxPrice, string? sortBy);
        Task<Product> CreateProductAsync(Product product);
        Task<Product> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(int id);
        Task<bool> UpdateStockAsync(int productId, int quantityChange);
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
        Task<Category?> GetCategoryByIdAsync(int id);
    }
}
