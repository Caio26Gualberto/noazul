using NoAzul.Application.AuditLogs.Dtos;
using NoAzul.Application.Pagination;
using NoAzul.Application.Response;

namespace NoAzul.Application.AuditLogs.Interfaces
{
    public interface IAuditLogAppService
    {
        Task<Result<PagedResultDto<AuditLogListItemDto>>> GetAuditLogs(AuditLogQueryDto query);
        Task<Result<AuditLogFiltersDto>> GetFilters();
    }
}
