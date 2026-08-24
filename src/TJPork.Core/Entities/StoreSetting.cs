using System.ComponentModel.DataAnnotations;

namespace TJPork.Core.Entities
{
    public class StoreSetting
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        [StringLength(250)]
        public string Description { get; set; } = string.Empty;

        [StringLength(50)]
        public string Group { get; set; } = "General"; // General, Delivery, Notifications, Payment
    }
}
