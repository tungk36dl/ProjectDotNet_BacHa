using BacHa.Models;

namespace BacHa.HelperServices
{
    public interface IDataSeedService
    {
        Task SeedDefaultAdminUserAsync();
    }
}
