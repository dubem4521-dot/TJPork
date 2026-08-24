using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using TJPork.Core.Entities;

namespace TJPork.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public string? DeliveryAddress { get; set; }

        public string? City { get; set; }

        public string? PostalCode { get; set; }

        public string AvatarUrl { get; set; } = "/images/avatars/default.png";

        public bool IsBlocked { get; set; } = false;

        public string? AdminNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<Address> SavedAddresses { get; set; } = new List<Address>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
