using Hangfire;
using NoAzul.Application.BankConnections.Interfaces;

namespace NoAzul.Workers.Triggers.Recurring.BankConnections
{
    public class BankConnectionRecurringJobsTrigger
    {
        private readonly IBankConnectionAppService _appService;

        public BankConnectionRecurringJobsTrigger(IBankConnectionAppService appService)
        {
            _appService = appService;
        }

        [Queue("bank-connections")]
        [JobDisplayName("Bancos: sincronização diária")]
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 900, 3600 })]
        [DisableConcurrentExecution(timeoutInSeconds: 30)]
        public async Task SyncAllAsync()
        {
            await _appService.SyncAll();
        }
    }
}
