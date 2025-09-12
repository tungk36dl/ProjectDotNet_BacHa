using Microsoft.EntityFrameworkCore;



namespace BacHa.Models
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }
    //public DbSet<Product> Products { get; set; }
    }

}
