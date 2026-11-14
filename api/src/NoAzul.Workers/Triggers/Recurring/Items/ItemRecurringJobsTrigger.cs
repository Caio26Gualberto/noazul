using Hangfire;
using NoAzul.Application.Items.Interfaces;

namespace NoAzul.Workers.Triggers.Recurring.Items
{
    public class ItemRecurringJobsTrigger
    {
        private readonly IItemAppService _appService;

        public ItemRecurringJobsTrigger(IItemAppService appService)
        {
            _appService = appService;
        }

        [Queue("items")]
        [JobDisplayName("Itens: arquivar vencidos")]
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 60, 600 })]
        [DisableConcurrentExecution(timeoutInSeconds: 30)]
        public async Task ArchiveExpiredAsync()
        {
            await _appService.ArchiveExpired();
        }
    }
}
