using Microsoft.EntityFrameworkCore;
using NoAzul.Application.Transactions.Interfaces;
using NoAzul.Domain.Entities;
using NoAzul.Domain.Enums.Transactions;
using NoAzul.Infra.Data.Context;

namespace NoAzul.Infra.Data.Repositories.Transactions
{
    public class TransactionRepository : Repository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Dictionary<ETransactionCategory, decimal>> SumByCategoryAsync(
            int userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(t => t.UserId == userId
                    && !t.IsDeleted
                    && !t.IsIgnored
                    && t.OccurredAt >= fromUtc
                    && t.OccurredAt < toUtc)
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key, Total = g.Sum(t => t.Amount) })
                .ToDictionaryAsync(x => x.Category, x => x.Total, ct);
        }

        public async Task<int> ApplyCategoryRuleAsync(
            int userId, string description, ETransactionCategory category, bool isIgnored, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(t => t.UserId == userId && !t.IsDeleted && t.Description == description)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.Category, category)
                    .SetProperty(t => t.IsIgnored, isIgnored), ct);
        }
    }
}
