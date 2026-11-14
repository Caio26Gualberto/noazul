using NoAzul.Application.BackgroundJobs.Dtos;
using NoAzul.Domain.Enums.Jobs;

namespace NoAzul.Application.BackgroundJobs.Interfaces
{
    public interface IBackgroundJobHandler
    {
        EBackgroundJobType JobType { get; }
        Task<BackgroundJobExecutionResultDto> ExecuteAsync(string? payloadJson, CancellationToken ct = default);
    }
}
