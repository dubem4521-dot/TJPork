using System;
using System.ComponentModel.DataAnnotations;

namespace TJPork.Core.Entities
{
    public class Notification
    {
        public int Id { get; set; }

        public string? UserId { get; set; } // Nullable if system-wide or specific to user

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [StringLength(250)]
        public string? LinkUrl { get; set; }

        [StringLength(50)]
        public string IconClass { get; set; } = "fa-solid fa-bell";

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
