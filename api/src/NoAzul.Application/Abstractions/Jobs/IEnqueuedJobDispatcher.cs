using NoAzul.Domain.Enums.Jobs;

namespace NoAzul.Application.Abstractions.Jobs
{
    public interface IEnqueuedJobDispatcher
    {
        string EnqueueBackgroundJobRun(int backgroundJobRunId, EBackgroundJobType jobType);
        string EnqueueItemCreated(int itemId);
        string EnqueueBankConnectionSync(int bankConnectionId);
    }
}
