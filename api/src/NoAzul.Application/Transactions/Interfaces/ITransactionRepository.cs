using NoAzul.Application.Abstractions.Repository;
using NoAzul.Domain.Entities;
using NoAzul.Domain.Enums.Transactions;

namespace NoAzul.Application.Transactions.Interfaces
{
    public interface ITransactionRepository : IRepository<Transaction>
    {
        Task<List<CategoryTotal>> SumByCategoryAsync(
            int userId, DateTime fromUtc, DateTime toUtc, int? connectionId, CancellationToken ct = default);

        Task<int> ApplyCategoryRuleAsync(
            int userId,
            string description,
            ETransactionCategory category,
            int? customCategoryId,
            bool isIgnored,
            CancellationToken ct = default);

        Task<int> SoftDeleteByAccountsAsync(
            IReadOnlyCollection<int> financialAccountIds, int? deletedBy, CancellationToken ct = default);
    }

    public sealed record CategoryTotal(ETransactionCategory Category, int? CustomCategoryId, decimal Total);
}
