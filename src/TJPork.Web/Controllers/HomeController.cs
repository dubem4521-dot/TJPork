using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TJPork.Core.Interfaces;
using TJPork.Infrastructure.Data;
using TJPork.Web.Models;
using TJPork.Web.ViewModels;

namespace TJPork.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly TJPorkDbContext _db;

        public HomeController(IProductService productService, TJPorkDbContext db)
        {
            _productService = productService;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var featuredProducts = await _productService.GetFeaturedProductsAsync();
            var allProducts = await _productService.GetAllProductsAsync();
            var categories = await _productService.GetAllCategoriesAsync();
            var reviews = await _db.Reviews
                .Where(r => r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Take(8)
                .ToListAsync();

            var heroSlides = new List<HeroSlide>
            {
                new HeroSlide
                {
                    Title = "Artisanal Smoked Heritage Bacon",
                    Subtitle = "Crafted by Tinashe & Jeffery",
                    Description = "Naturally dry-cured for 7 days with Vermont maple syrup and cold-smoked over natural fragrant applewood.",
                    Badge = "Staff Selection",
                    ImageUrl = "/images/hero/hero-bacon.jpg",
                    ButtonText = "Shop Artisanal Bacon",
                    ButtonUrl = "/Shop?categoryId=1"
                },
                new HeroSlide
                {
                    Title = "Prime Heritage Pork Chops",
                    Subtitle = "Pasture-Raised Berkshire",
                    Description = "Thick-cut 1.5-inch bone-in chops boasting exceptional marbling, tenderness, and rich culinary flavor.",
                    Badge = "Butcher Special",
                    ImageUrl = "/images/hero/hero-chops.jpg",
                    ButtonText = "Explore Heritage Cuts",
                    ButtonUrl = "/Shop?categoryId=3"
                },
                new HeroSlide
                {
                    Title = "Handcrafted Gourmet Sausages",
                    Subtitle = "Small-Batch Master Recipes",
                    Description = "Stuffed in natural hog casings with farm-fresh herbs, toasted whole fennel, and roasted garlic.",
                    Badge = "New Seasonals",
                    ImageUrl = "/images/hero/hero-sausage.jpg",
                    ButtonText = "Taste the Craft",
                    ButtonUrl = "/Shop?categoryId=2"
                }
            };

            var viewModel = new HomeViewModel
            {
                HeroSlides = heroSlides,
                Categories = categories,
                FeaturedProducts = featuredProducts,
                AllProducts = allProducts,
                CustomerReviews = reviews,
                NewReview = new ReviewFormModel()
            };

            return View(viewModel);
        }

        public IActionResult About()
        {
            var model = new AboutViewModel
            {
                TinasheBio = "Co-founder & Head of Sourcing. Tinashe has spent over 12 years working directly with regenerative family farms. His commitment to ethical animal welfare, pasture-raised genetics, and transparent farm-to-table traceability ensures only the highest quality heritage pork reaches T&JPork customers.",
                JefferyBio = "Co-founder & Master Curer. Jeffery is a certified charcutier with deep expertise in traditional dry-curing, hardwood pit smoking, and custom spice blending. Every recipe at T&JPork is developed and tested by Jeffery to guarantee unbeatable tenderness and flavor."
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<object>());
            }

            var results = await _productService.SearchProductsAsync(query, null, null, null, null);
            var suggestions = results.Take(6).Select(p => new
            {
                id = p.Id,
                name = p.Name,
                slug = p.Slug,
                price = p.FinalPrice,
                category = p.Category?.Name,
                imageUrl = p.ImageUrl
            });

            return Json(suggestions);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
