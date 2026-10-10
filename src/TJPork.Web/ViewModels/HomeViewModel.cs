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

    public class FeedbackViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please enter your name")]
        [System.ComponentModel.DataAnnotations.Display(Name = "Your Name")]
        public string FullName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please enter your email address")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [System.ComponentModel.DataAnnotations.Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Phone(ErrorMessage = "Please enter a valid phone number")]
        [System.ComponentModel.DataAnnotations.Display(Name = "Phone Number (Optional)")]
        public string? PhoneNumber { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please select a category")]
        [System.ComponentModel.DataAnnotations.Display(Name = "Topic / Category")]
        public string Category { get; set; } = "General Feedback";

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please enter a subject")]
        [System.ComponentModel.DataAnnotations.Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Please write your message or feedback")]
        [System.ComponentModel.DataAnnotations.Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Range(1, 5)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Satisfaction Rating")]
        public int Rating { get; set; } = 5;
    }
}
