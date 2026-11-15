using Hangfire;
using NoAzul.Application.Abstractions.Jobs;
using NoAzul.Domain.Enums.Jobs;
using NoAzul.Workers.Triggers.Enqueued.BankConnections;

namespace NoAzul.Workers.Dispatch
{
    public class EnqueuedJobDispatcher : IEnqueuedJobDispatcher
    {
        public string EnqueueBackgroundJobRun(int backgroundJobRunId, EBackgroundJobType jobType) =>
            jobType switch
            {
                _ => throw new InvalidOperationException(
                    $"Nenhuma fila definida para o job {jobType}. Mapeie o tipo em {nameof(EnqueuedJobDispatcher)}."),
            };

        public string EnqueueBankConnectionSync(int bankConnectionId) =>
            BackgroundJob.Enqueue<BankConnectionsEnqueuedJobsTrigger>(t => t.SyncAsync(bankConnectionId));
    }
}
