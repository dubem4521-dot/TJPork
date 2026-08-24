using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TJPork.Core.Interfaces;
using TJPork.Infrastructure.Data;
using TJPork.Web.ViewModels;

namespace TJPork.Web.Controllers
{
    public class ShopController : Controller
    {
        private readonly IProductService _productService;
        private readonly TJPorkDbContext _db;

        public ShopController(IProductService productService, TJPorkDbContext db)
        {
            _productService = productService;
            _db = db;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId, decimal? minPrice, decimal? maxPrice, string? sortBy)
        {
            var products = await _productService.SearchProductsAsync(search, categoryId, minPrice, maxPrice, sortBy);
            var categories = await _productService.GetAllCategoriesAsync();

            var viewModel = new ShopViewModel
            {
                Products = products,
                Categories = categories,
                SearchTerm = search,
                SelectedCategoryId = categoryId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy,
                TotalCount = products.Count()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(string slug)
        {
            if (string.IsNullOrEmpty(slug)) return NotFound();

            var product = await _productService.GetProductBySlugAsync(slug);
            if (product == null) return NotFound();

            var relatedProducts = await _productService.GetProductsByCategoryAsync(product.Category?.Slug ?? "");
            var filteredRelated = relatedProducts.Where(p => p.Id != product.Id).Take(4).ToList();

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = filteredRelated,
                NewReview = new ReviewFormModel { ProductId = product.Id }
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> QuickView(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            return PartialView("_ProductQuickView", product);
        }
    }
}
