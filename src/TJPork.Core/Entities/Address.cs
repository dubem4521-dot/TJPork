using System;
using System.ComponentModel.DataAnnotations;

namespace TJPork.Core.Entities
{
    public class Address
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Label { get; set; } = "Home"; // e.g. Home, Office

        [Required]
        [StringLength(100)]
        public string RecipientName { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string StreetAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
