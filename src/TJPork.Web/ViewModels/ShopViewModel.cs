using System.Collections.Generic;
using TJPork.Core.Entities;

namespace TJPork.Web.ViewModels
{
    public class ShopViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public string? SearchTerm { get; set; }
        public int? SelectedCategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SortBy { get; set; }
        public int TotalCount { get; set; }
    }

    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = null!;
        public IEnumerable<Product> RelatedProducts { get; set; } = new List<Product>();
        public ReviewFormModel NewReview { get; set; } = new ReviewFormModel();
    }

    public class AboutViewModel
    {
        public string TinasheBio { get; set; } = string.Empty;
        public string JefferyBio { get; set; } = string.Empty;
    }
}
