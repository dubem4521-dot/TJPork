using System.Collections.Generic;
using TJPork.Core.Entities;

namespace TJPork.Web.ViewModels
{
    public class HeroSlide
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Badge { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string ButtonText { get; set; } = "Shop Now";
        public string ButtonUrl { get; set; } = "/Shop";
    }

    public class HomeViewModel
    {
        public List<HeroSlide> HeroSlides { get; set; } = new List<HeroSlide>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public IEnumerable<Product> FeaturedProducts { get; set; } = new List<Product>();
        public IEnumerable<Product> AllProducts { get; set; } = new List<Product>();
        public IEnumerable<Review> CustomerReviews { get; set; } = new List<Review>();
        public ReviewFormModel NewReview { get; set; } = new ReviewFormModel();
    }

    public class ReviewFormModel
    {
        public int ProductId { get; set; }
        public int Rating { get; set; } = 5;
        public string Title { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
    }
}
