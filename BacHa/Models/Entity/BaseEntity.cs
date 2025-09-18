namespace BacHa.Models.Entity
{
    public class BaseEntity : DomainEntity<Guid>
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set;}
    }
}

