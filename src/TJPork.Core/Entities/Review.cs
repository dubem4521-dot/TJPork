using System;
using System.ComponentModel.DataAnnotations;

namespace TJPork.Core.Entities
{
    public class Review
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public virtual Product? Product { get; set; }

        public string? UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(150)]
        public string? CustomerEmail { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;

        public bool IsVerifiedBuyer { get; set; } = true;

        public bool IsApproved { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
