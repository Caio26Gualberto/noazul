using Microsoft.EntityFrameworkCore;
using NoAzul.Application.Items.Dtos;
using NoAzul.Application.Items.Interfaces;
using NoAzul.Domain.Entities;
using NoAzul.Domain.Enums.Items;
using NoAzul.Infra.Data.Context;

namespace NoAzul.Infra.Data.Repositories.Items
{
    public class ItemRepository : Repository<Item>, IItemRepository
    {
        public ItemRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<bool> TryActivateAsync(int itemId, int userId, CancellationToken ct = default)
        {
            var affected = await _dbSet
                .Where(i => i.Id == itemId
                    && i.UserId == userId
                    && !i.IsDeleted
                    && i.Status == EItemStatus.Draft)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, EItemStatus.Active), ct);

            return affected == 1;
        }

        public async Task<bool> TryArchiveAsync(int itemId, int? userId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            var affected = await _dbSet
                .Where(i => i.Id == itemId
                    && (userId == null || i.UserId == userId)
                    && !i.IsDeleted
                    && i.Status != EItemStatus.Archived)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(i => i.Status, EItemStatus.Archived)
                        .SetProperty(i => i.ArchivedAt, now),
                    ct);

            return affected == 1;
        }

        public async Task<int> ArchiveExpiredAsync(DateTime nowUtc, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(i => !i.IsDeleted
                    && i.Status != EItemStatus.Archived
                    && i.ExpiresAt != null
                    && i.ExpiresAt <= nowUtc)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(i => i.Status, EItemStatus.Archived)
                        .SetProperty(i => i.ArchivedAt, nowUtc),
                    ct);
        }

        public async Task<ItemsSnapshotDto> SnapshotAsync(CancellationToken ct = default)
        {
            var counts = await _dbSet
                .Where(i => !i.IsDeleted)
                .GroupBy(i => i.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            return new ItemsSnapshotDto
            {
                Draft = counts.FirstOrDefault(c => c.Status == EItemStatus.Draft)?.Count ?? 0,
                Active = counts.FirstOrDefault(c => c.Status == EItemStatus.Active)?.Count ?? 0,
                Archived = counts.FirstOrDefault(c => c.Status == EItemStatus.Archived)?.Count ?? 0,
                TakenAt = DateTime.UtcNow
            };
        }
    }
}
