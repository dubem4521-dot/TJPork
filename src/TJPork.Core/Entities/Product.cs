using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TJPork.Core.Entities
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string Slug { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public virtual Category? Category { get; set; }

        [Required]
        [StringLength(150)]
        public string ShortDescription { get; set; } = string.Empty;

        [Required]
        public string LongDescription { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 10000.00)]
        public decimal Price { get; set; }

        [Range(0, 100)]
        public int DiscountPercentage { get; set; } = 0;

        [NotMapped]
        public decimal FinalPrice => DiscountPercentage > 0 
            ? Math.Round(Price * (1 - (decimal)DiscountPercentage / 100m), 2) 
            : Price;

        [Required]
        [Range(0, 10000)]
        public int StockQuantity { get; set; } = 0;

        [StringLength(500)]
        public string ImageUrl { get; set; } = "/images/products/placeholder.jpg";

        public bool IsFeatured { get; set; } = false;

        public bool IsActive { get; set; } = true;

        [Range(0.0, 5.0)]
        public double Rating { get; set; } = 5.0;

        public int ReviewCount { get; set; } = 0;

        [StringLength(100)]
        public string WeightDescription { get; set; } = "500g (approx.)";

        [StringLength(200)]
        public string CuringMethod { get; set; } = "Hardwood smoked & dry aged";

        [StringLength(200)]
        public string OriginFarm { get; set; } = "Pasture-Raised Heritage Farms";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
