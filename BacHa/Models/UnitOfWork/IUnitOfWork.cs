using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BacHa.Models.Entity;
using BacHa.Models.Service;

namespace BacHa.Models.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        Task SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }
}
