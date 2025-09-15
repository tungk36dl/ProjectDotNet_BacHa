using System.ComponentModel.DataAnnotations;

namespace BacHa.Models.Entity
{
    public abstract class DomainEntity<TKey>
    {
        [Key]
        public TKey Id { get; set; }
    }
}
