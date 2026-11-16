using NoAzul.Application.Pagination;
using NoAzul.Application.Response;
using NoAzul.Application.Transactions.Dtos;
using NoAzul.Domain.Entities;

namespace NoAzul.Application.Transactions.Interfaces
{
    public interface ITransactionAppService
    {
        Task<Result<PagedResultDto<TransactionDto>>> GetMine(TransactionListQueryDto query);
        Task<Result<List<MonthSummaryDto>>> GetMonthlySummary(int months, int? connectionId);
        Task<Result<CategoryRuleResultDto>> UpdateCategory(int id, UpdateTransactionCategoryInputDto input);
        Task<Result<DisplayNameResultDto>> UpdateDisplayName(int id, UpdateDisplayNameInputDto input);

        Task<int> SyncFromProvider(FinancialAccount account, int userId);
        Task<int> RemoveForAccounts(IReadOnlyCollection<int> financialAccountIds);
        Task<int> ClearCustomCategory(int customCategoryId);
    }
}
