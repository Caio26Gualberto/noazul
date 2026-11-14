using NoAzul.Application.Abstractions.Repository;
using NoAzul.Application.Items.Dtos;
using NoAzul.Domain.Entities;

namespace NoAzul.Application.Items.Interfaces
{
    public interface IItemRepository : IRepository<Item>
    {
        Task<bool> TryActivateAsync(int itemId, int userId, CancellationToken ct = default);
        Task<bool> TryArchiveAsync(int itemId, int? userId, CancellationToken ct = default);
        Task<int> ArchiveExpiredAsync(DateTime nowUtc, CancellationToken ct = default);
        Task<ItemsSnapshotDto> SnapshotAsync(CancellationToken ct = default);
    }
}
