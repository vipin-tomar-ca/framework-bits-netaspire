using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace IntegrationPlatform.Infrastructure.Data
{
    public class TransactionManager
    {
        private readonly ApplicationDbContext _context;

        public TransactionManager(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await action();
                await transaction.CommitAsync();
                return result;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
} 