namespace BacHa.Models.Entity
{
    public class BaseEntity : DomainEntity<Guid>
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}

