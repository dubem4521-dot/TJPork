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
        public string HeaderSuperTitle { get; set; } = "The Artisanal Standard";
        public string HeaderTitle { get; set; } = "Two Founders. One Passion for Premium Pork.";
        public string HeaderIntro { get; set; } = "T&JPork was born out of frustration with industrial supermarket pork—watery cuts, bland flavor, and questionable farm practices. Tinashe and Jeffery set out to restore heritage pork to its rightful glory.";

        public string Founder1Name { get; set; } = "Tinashe";
        public string Founder1Role { get; set; } = "Co-Founder & Head of Ethical Sourcing";
        public string Founder1Bio { get; set; } = "Tinashe has spent over 12 years working directly with regenerative family farms. His commitment to ethical animal welfare, pasture-raised genetics, and transparent farm-to-table traceability ensures only the highest quality heritage pork reaches T&JPork customers.";
        public string Founder1ImageUrl { get; set; } = "/images/about/tinashe.jpg";
        public string Founder1Badge1 { get; set; } = "Pasture Genetics";
        public string Founder1Badge2 { get; set; } = "Farm Direct";

        public string Founder2Name { get; set; } = "Jeffery";
        public string Founder2Role { get; set; } = "Co-Founder & Master Charcutier";
        public string Founder2Bio { get; set; } = "Jeffery is a certified charcutier with deep expertise in traditional dry-curing, hardwood pit smoking, and custom spice blending. Every recipe at T&JPork is developed and tested by Jeffery to guarantee unbeatable tenderness and flavor.";
        public string Founder2ImageUrl { get; set; } = "/images/about/jeffery.jpg";
        public string Founder2Badge1 { get; set; } = "Hardwood Smoke";
        public string Founder2Badge2 { get; set; } = "Small-Batch Spices";

        public string Value1Title { get; set; } = "1. Heritage Quality Sourcing";
        public string Value1Description { get; set; } = "We exclusively partner with farms raising pure Berkshire, Duroc, and Red Wattle hogs with deep red meat color and natural intra-muscular marbling.";

        public string Value2Title { get; set; } = "2. 100% Sustainable Practices";
        public string Value2Description { get; set; } = "Rotational grazing, zero preventative antibiotics, non-GMO forage, and whole-animal butcher philosophy to eliminate waste.";

        public string Value3Title { get; set; } = "3. Community Commitment";
        public string Value3Description { get; set; } = "Supporting local family-owned butcheries and farm co-ops. A percentage of all seasonal proceeds supports regional agricultural apprenticeships.";

        public string MissionTitle { get; set; } = "To Deliver Unrivaled Pork Excellence";
        public string MissionQuote { get; set; } = "Every cut that leaves our smokehouse represents our names and our reputation. If you don't taste the difference in the first bite, we will make it right.";
        public string MissionAttribution { get; set; } = "Tinashe & Jeffery";

        // Aliases for backwards compatibility
        public string TinasheBio
        {
            get => Founder1Bio;
            set => Founder1Bio = value;
        }

        public string JefferyBio
        {
            get => Founder2Bio;
            set => Founder2Bio = value;
        }
    }
}
