using System.Text.Json;
using NoAzul.Application.BackgroundJobs.Dtos;
using NoAzul.Application.BackgroundJobs.Interfaces;
using NoAzul.Application.Items.Interfaces;
using NoAzul.Domain.Enums.Jobs;

namespace NoAzul.Application.BackgroundJobs.Handlers
{
    public class ItemsSnapshotJobHandler : IBackgroundJobHandler
    {
        private readonly IItemAppService _items;

        public ItemsSnapshotJobHandler(IItemAppService items)
        {
            _items = items;
        }

        public EBackgroundJobType JobType => EBackgroundJobType.ItemsSnapshot;

        public async Task<BackgroundJobExecutionResultDto> ExecuteAsync(string? payloadJson, CancellationToken ct = default)
        {
            var snapshot = await _items.Snapshot();

            return new BackgroundJobExecutionResultDto
            {
                ResultJson = JsonSerializer.Serialize(snapshot)
            };
        }
    }
}
