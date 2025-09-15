using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using BacHa.Models.Service;
using BacHa.Models;
using BacHa.Models.Entity;

namespace BacHa.Models.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDBContext _context;
        private IDbContextTransaction _transaction;


        public UnitOfWork(ApplicationDBContext context)
        {
            _context = context;
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();

        }

        public async Task CommitAsync()
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
        }

        public async Task RollbackAsync()
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

 
    }
}
