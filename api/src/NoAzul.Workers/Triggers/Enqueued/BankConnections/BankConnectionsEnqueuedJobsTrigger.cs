using Hangfire;
using NoAzul.Application.BankConnections.Interfaces;

namespace NoAzul.Workers.Triggers.Enqueued.BankConnections
{
    public class BankConnectionsEnqueuedJobsTrigger
    {
        private readonly IBankConnectionAppService _appService;

        public BankConnectionsEnqueuedJobsTrigger(IBankConnectionAppService appService)
        {
            _appService = appService;
        }

        [Queue("bank-connections")]
        [JobDisplayName("Bancos: sincronizar conexão #{0}")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 1800 })]
        public async Task SyncAsync(int bankConnectionId)
        {
            await _appService.Sync(bankConnectionId);
        }
    }
}
