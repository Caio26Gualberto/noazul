using NoAzul.Application.BackgroundJobs.Dtos;
using NoAzul.Application.Pagination;
using NoAzul.Application.Response;
using NoAzul.Domain.Enums.Jobs;

namespace NoAzul.Application.BackgroundJobs.Interfaces
{
    public interface IBackgroundJobAppService
    {
        Task<Result<TriggeredBackgroundJobDto>> Trigger(EBackgroundJobType jobType);
        Task<Result<TriggeredBackgroundJobDto>> Retry(int id);
        Task<Result<PagedResultDto<BackgroundJobListItemDto>>> GetRuns(BackgroundJobQueryDto query);
        Task<Result<BackgroundJobDetailDto>> GetRun(int id);
        Task<Result<List<RecurringJobDto>>> GetRecurring();
        Task ExecuteAsync(int runId, CancellationToken ct = default);
    }
}
