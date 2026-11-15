using NoAzul.Application.Pagination;
using NoAzul.Domain.Enums.Transactions;

namespace NoAzul.Application.Transactions.Dtos
{
    public class TransactionListQueryDto : PagedRequestDto
    {
        public string? Month { get; set; }
        public ETransactionOrigin? Origin { get; set; }
        public string? Category { get; set; }
        public string? Search { get; set; }
    }
}
