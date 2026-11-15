using NoAzul.Application.Abstractions.Repository;
using NoAzul.Domain.Entities;
using NoAzul.Domain.Enums.Transactions;

namespace NoAzul.Application.Transactions.Interfaces
{
    public interface ITransactionRepository : IRepository<Transaction>
    {
        Task<Dictionary<ETransactionCategory, decimal>> SumByCategoryAsync(
            int userId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

        Task<int> ApplyCategoryRuleAsync(
            int userId, string description, ETransactionCategory category, bool isIgnored, CancellationToken ct = default);

        Task<int> SoftDeleteByAccountsAsync(
            IReadOnlyCollection<int> financialAccountIds, int? deletedBy, CancellationToken ct = default);
    }
}
