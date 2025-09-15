using System.ComponentModel.DataAnnotations;
using BacHa.Models.Entity;

namespace BacHa.Models
{
    public class Role : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}

