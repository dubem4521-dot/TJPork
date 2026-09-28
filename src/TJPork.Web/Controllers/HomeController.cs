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

            var heroSlides = new List<HeroSlide>();
            if (featuredProducts.Any())
            {
                foreach (var product in featuredProducts.Take(4))
                {
                    heroSlides.Add(new HeroSlide
                    {
                        Title = product.Name,
                        Subtitle = product.Category?.Name ?? "Heritage Artisanal Cut",
                        Description = product.ShortDescription,
                        Badge = product.DiscountPercentage > 0 ? $"{product.DiscountPercentage}% Off" : "Butcher Special",
                        ImageUrl = !string.IsNullOrEmpty(product.ImageUrl) ? product.ImageUrl : "/images/hero/hero-bacon.jpg",
                        ButtonText = $"Shop Now &bull; R{product.FinalPrice:F2}",
                        ButtonUrl = $"/Shop/Details?slug={product.Slug}"
                    });
                }
            }
            else
            {
                heroSlides.Add(new HeroSlide
                {
                    Title = "Artisanal Heritage Pork",
                    Subtitle = "Pasture-Raised & Hand-Cured",
                    Description = "Ethically sourced small-batch cuts prepared by master curers in the Western Cape and KZN Midlands.",
                    Badge = "Smokehouse Selection",
                    ImageUrl = "/images/hero/hero-bacon.jpg",
                    ButtonText = "Explore Catalog",
                    ButtonUrl = "/Shop"
                });
            }

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

        public async Task<IActionResult> About()
        {
            var settings = await _db.StoreSettings
                .Where(s => s.Group == "AboutUs")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            var model = new AboutViewModel();

            if (settings.TryGetValue("AboutUs.HeaderSuperTitle", out var superTitle)) model.HeaderSuperTitle = superTitle;
            if (settings.TryGetValue("AboutUs.HeaderTitle", out var title)) model.HeaderTitle = title;
            if (settings.TryGetValue("AboutUs.HeaderIntro", out var intro)) model.HeaderIntro = intro;

            if (settings.TryGetValue("AboutUs.Founder1Name", out var f1Name)) model.Founder1Name = f1Name;
            if (settings.TryGetValue("AboutUs.Founder1Role", out var f1Role)) model.Founder1Role = f1Role;
            if (settings.TryGetValue("AboutUs.Founder1Bio", out var f1Bio)) model.Founder1Bio = f1Bio;
            if (settings.TryGetValue("AboutUs.Founder1ImageUrl", out var f1Img)) model.Founder1ImageUrl = f1Img;
            if (settings.TryGetValue("AboutUs.Founder1Badge1", out var f1B1)) model.Founder1Badge1 = f1B1;
            if (settings.TryGetValue("AboutUs.Founder1Badge2", out var f1B2)) model.Founder1Badge2 = f1B2;

            if (settings.TryGetValue("AboutUs.Founder2Name", out var f2Name)) model.Founder2Name = f2Name;
            if (settings.TryGetValue("AboutUs.Founder2Role", out var f2Role)) model.Founder2Role = f2Role;
            if (settings.TryGetValue("AboutUs.Founder2Bio", out var f2Bio)) model.Founder2Bio = f2Bio;
            if (settings.TryGetValue("AboutUs.Founder2ImageUrl", out var f2Img)) model.Founder2ImageUrl = f2Img;
            if (settings.TryGetValue("AboutUs.Founder2Badge1", out var f2B1)) model.Founder2Badge1 = f2B1;
            if (settings.TryGetValue("AboutUs.Founder2Badge2", out var f2B2)) model.Founder2Badge2 = f2B2;

            if (settings.TryGetValue("AboutUs.Value1Title", out var v1T)) model.Value1Title = v1T;
            if (settings.TryGetValue("AboutUs.Value1Description", out var v1D)) model.Value1Description = v1D;
            if (settings.TryGetValue("AboutUs.Value2Title", out var v2T)) model.Value2Title = v2T;
            if (settings.TryGetValue("AboutUs.Value2Description", out var v2D)) model.Value2Description = v2D;
            if (settings.TryGetValue("AboutUs.Value3Title", out var v3T)) model.Value3Title = v3T;
            if (settings.TryGetValue("AboutUs.Value3Description", out var v3D)) model.Value3Description = v3D;

            if (settings.TryGetValue("AboutUs.MissionTitle", out var mT)) model.MissionTitle = mT;
            if (settings.TryGetValue("AboutUs.MissionQuote", out var mQ)) model.MissionQuote = mQ;
            if (settings.TryGetValue("AboutUs.MissionAttribution", out var mA)) model.MissionAttribution = mA;

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
