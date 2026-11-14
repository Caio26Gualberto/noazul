using Hangfire;
using NoAzul.Application.BackgroundJobs.Interfaces;

namespace NoAzul.Workers.Triggers.Enqueued.BackgroundJobs
{
    public class BackgroundJobsEnqueuedTrigger
    {
        private readonly IBackgroundJobAppService _appService;

        public BackgroundJobsEnqueuedTrigger(IBackgroundJobAppService appService)
        {
            _appService = appService;
        }

        [Queue("items")]
        [JobDisplayName("Itens: snapshot (manual) #{0}")]
        [AutomaticRetry(Attempts = 0)]
        public async Task RunItemsSnapshotAsync(int backgroundJobRunId)
        {
            await _appService.ExecuteAsync(backgroundJobRunId);
        }
    }
}
