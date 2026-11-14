using Hangfire;
using NoAzul.Application.Abstractions.Logging;

namespace NoAzul.Workers.Triggers.Enqueued.Items
{
    public class ItemsEnqueuedJobsTrigger
    {
        private readonly IApplicationLogger _logger;

        public ItemsEnqueuedJobsTrigger(IApplicationLoggerFactory loggerFactory)
        {
            _logger = loggerFactory.Create(LogContext.Items);
        }

        [Queue("items")]
        [JobDisplayName("Itens: pós-criação #{0}")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 120, 600 })]
        public Task OnItemCreatedAsync(int itemId)
        {
            _logger.SetOperation("OnItemCreated");

            _logger.Info(
                eventName: "ITEM_CREATED_PROCESSED",
                message: "Follow-up after item creation ran",
                extra: new { ItemId = itemId });

            return Task.CompletedTask;
        }
    }
}
