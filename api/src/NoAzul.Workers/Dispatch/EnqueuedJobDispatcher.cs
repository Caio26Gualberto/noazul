using Hangfire;
using NoAzul.Application.Abstractions.Jobs;
using NoAzul.Domain.Enums.Jobs;
using NoAzul.Workers.Triggers.Enqueued.BackgroundJobs;
using NoAzul.Workers.Triggers.Enqueued.Items;

namespace NoAzul.Workers.Dispatch
{
    public class EnqueuedJobDispatcher : IEnqueuedJobDispatcher
    {
        public string EnqueueBackgroundJobRun(int backgroundJobRunId, EBackgroundJobType jobType) =>
            jobType switch
            {
                EBackgroundJobType.ItemsSnapshot =>
                    BackgroundJob.Enqueue<BackgroundJobsEnqueuedTrigger>(t => t.RunItemsSnapshotAsync(backgroundJobRunId)),

                _ => throw new InvalidOperationException(
                    $"Nenhuma fila definida para o job {jobType}. Mapeie o tipo em {nameof(EnqueuedJobDispatcher)}."),
            };

        public string EnqueueItemCreated(int itemId) =>
            BackgroundJob.Enqueue<ItemsEnqueuedJobsTrigger>(t => t.OnItemCreatedAsync(itemId));
    }
}
