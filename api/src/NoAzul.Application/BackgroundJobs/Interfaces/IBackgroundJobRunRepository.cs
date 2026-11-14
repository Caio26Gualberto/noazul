using NoAzul.Application.Abstractions.Repository;
using NoAzul.Domain.Entities;

namespace NoAzul.Application.BackgroundJobs.Interfaces
{
    public interface IBackgroundJobRunRepository : IRepository<BackgroundJobRun>
    {
        Task<bool> TryClaimAsync(int runId, CancellationToken ct = default);
        Task ReleaseDeduplicationKeyAsync(int runId, CancellationToken ct = default);
    }
}
