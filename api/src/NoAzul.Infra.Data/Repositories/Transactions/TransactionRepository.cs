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

        public async Task<List<CategoryTotal>> SumByCategoryAsync(
            int userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(t => t.UserId == userId
                    && !t.IsDeleted
                    && !t.IsIgnored
                    && t.OccurredAt >= fromUtc
                    && t.OccurredAt < toUtc)
                .GroupBy(t => new { t.Category, t.CustomCategoryId })
                .Select(g => new CategoryTotal(g.Key.Category, g.Key.CustomCategoryId, g.Sum(t => t.Amount)))
                .ToListAsync(ct);
        }

        public async Task<int> ApplyCategoryRuleAsync(
            int userId,
            string description,
            ETransactionCategory category,
            int? customCategoryId,
            bool isIgnored,
            CancellationToken ct = default)
        {
            var query = _dbSet.Where(t => t.UserId == userId && !t.IsDeleted && t.Description == description);

            if (customCategoryId.HasValue)
                return await query.ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.CustomCategoryId, customCategoryId)
                    .SetProperty(t => t.IsIgnored, isIgnored), ct);

            return await query.ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Category, category)
                .SetProperty(t => t.CustomCategoryId, (int?)null)
                .SetProperty(t => t.IsIgnored, isIgnored), ct);
        }

        public async Task<int> SoftDeleteByAccountsAsync(
            IReadOnlyCollection<int> financialAccountIds, int? deletedBy, CancellationToken ct = default)
        {
            if (financialAccountIds.Count == 0)
                return 0;

            var now = DateTime.UtcNow;
            var ids = financialAccountIds.ToList();

            return await _dbSet
                .Where(t => t.FinancialAccountId.HasValue && ids.Contains(t.FinancialAccountId.Value) && !t.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(t => t.IsDeleted, true)
                    .SetProperty(t => t.DeletedAt, now)
                    .SetProperty(t => t.DeletedBy, deletedBy), ct);
        }
    }
}
