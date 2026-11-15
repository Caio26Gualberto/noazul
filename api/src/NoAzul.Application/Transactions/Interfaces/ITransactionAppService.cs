using NoAzul.Application.Pagination;
using NoAzul.Application.Response;
using NoAzul.Application.Transactions.Dtos;
using NoAzul.Domain.Entities;

namespace NoAzul.Application.Transactions.Interfaces
{
    public interface ITransactionAppService
    {
        Task<Result<PagedResultDto<TransactionDto>>> GetMine(TransactionListQueryDto query);
        Task<Result<List<MonthSummaryDto>>> GetMonthlySummary(int months);

        Task<int> SyncFromProvider(FinancialAccount account, int userId);
    }
}
