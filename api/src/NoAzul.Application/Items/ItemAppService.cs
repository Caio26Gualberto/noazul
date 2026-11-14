using NoAzul.Application.Abstractions.Authentication;
using NoAzul.Application.Abstractions.Jobs;
using NoAzul.Application.Abstractions.Logging;
using NoAzul.Application.Abstractions.Repository;
using NoAzul.Application.Common.Time;
using NoAzul.Application.Items.Dtos;
using NoAzul.Application.Items.Interfaces;
using NoAzul.Application.Pagination;
using NoAzul.Application.Response;
using NoAzul.Domain.Entities;
using NoAzul.Domain.Enums.Items;

namespace NoAzul.Application.Items
{
    public class ItemAppService : IItemAppService
    {
        private const int NameMinLength = 2;
        private const int NameMaxLength = 120;
        private const int DescriptionMaxLength = 1000;

        private readonly IItemRepository _repository;
        private readonly IEnqueuedJobDispatcher _dispatcher;
        private readonly ICurrentUser _currentUser;
        private readonly IApplicationLogger _logger;

        public ItemAppService(
            IItemRepository repository,
            IEnqueuedJobDispatcher dispatcher,
            ICurrentUser currentUser,
            IApplicationLoggerFactory loggerFactory)
        {
            _repository = repository;
            _dispatcher = dispatcher;
            _currentUser = currentUser;
            _logger = loggerFactory.Create(LogContext.Items);
        }

        public Task<Result<PagedResultDto<ItemDto>>> GetMine(ItemListQueryDto query) =>
            ListAsync(query, _currentUser.UserId);

        public async Task<Result<ItemDto>> GetMineById(int id)
        {
            var item = await FindAsync(id, _currentUser.UserId, asNoTracking: true);

            if (item == null)
                return Result<ItemDto>.Fail(NotFound());

            return Result<ItemDto>.Ok(Map(item), "ITEM_FOUND", "Item found successfully");
        }

        public async Task<Result<ItemDto>> Create(CreateItemInputDto input)
        {
            _logger.SetOperation("CreateItem");

            var validation = Validate(input.Name, input.Description, input.ExpiresAt);

            if (validation != null)
                return Result<ItemDto>.Fail(validation);

            if (await NameTakenAsync(input.Name, exceptId: null))
                return Result<ItemDto>.Fail(NameTaken());

            var item = new Item
            {
                UserId = _currentUser.UserId,
                Name = input.Name.Trim(),
                Description = Normalize(input.Description),
                ExpiresAt = input.ExpiresAt,
                Status = EItemStatus.Draft
            };

            await _repository.AddAsync(item);

            _logger.Set("ItemId", item.Id);

            try
            {
                _dispatcher.EnqueueItemCreated(item.Id);
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    eventName: "ITEM_CREATED_JOB_NOT_ENQUEUED",
                    message: "Item was created but the follow-up job could not be enqueued",
                    userId: item.UserId,
                    extra: new { ItemId = item.Id, Error = ex.Message });
            }

            return Result<ItemDto>.Ok(Map(item), "ITEM_CREATED", "Item created successfully");
        }

        public async Task<Result<ItemDto>> Update(int id, UpdateItemInputDto input)
        {
            var validation = Validate(input.Name, input.Description, input.ExpiresAt);

            if (validation != null)
                return Result<ItemDto>.Fail(validation);

            var item = await FindAsync(id, _currentUser.UserId, asNoTracking: false);

            if (item == null)
                return Result<ItemDto>.Fail(NotFound());

            if (item.Status == EItemStatus.Archived)
                return Result<ItemDto>.Fail(
                    new Error("ITEM_ARCHIVED", "An archived item cannot be edited", ErrorType.Validation));

            if (await NameTakenAsync(input.Name, exceptId: id))
                return Result<ItemDto>.Fail(NameTaken());

            item.Name = input.Name.Trim();
            item.Description = Normalize(input.Description);
            item.ExpiresAt = input.ExpiresAt;

            await _repository.UpdateAsync(item);

            return Result<ItemDto>.Ok(Map(item), "ITEM_UPDATED", "Item updated successfully");
        }

        public async Task<Result<ItemDto>> Activate(int id)
        {
            var activated = await _repository.TryActivateAsync(id, _currentUser.UserId);

            if (!activated)
                return Result<ItemDto>.Fail(
                    new Error("ITEM_NOT_ACTIVATABLE", "Only draft items can be activated", ErrorType.Conflict));

            var item = await FindAsync(id, _currentUser.UserId, asNoTracking: true);

            if (item == null)
                return Result<ItemDto>.Fail(NotFound());

            return Result<ItemDto>.Ok(Map(item), "ITEM_ACTIVATED", "Item activated successfully");
        }

        public async Task<Result<bool>> ArchiveMine(int id) =>
            await ArchiveAsync(id, _currentUser.UserId);

        public Task<Result<PagedResultDto<ItemDto>>> GetAll(ItemListQueryDto query) =>
            ListAsync(query, userId: null);

        public async Task<Result<ItemDto>> GetById(int id)
        {
            var item = await FindAsync(id, userId: null, asNoTracking: true);

            if (item == null)
                return Result<ItemDto>.Fail(NotFound());

            return Result<ItemDto>.Ok(Map(item), "ITEM_FOUND", "Item found successfully");
        }

        public async Task<Result<bool>> Archive(int id) =>
            await ArchiveAsync(id, userId: null);

        public async Task<Result<bool>> Delete(int id)
        {
            var item = await FindAsync(id, userId: null, asNoTracking: false);

            if (item == null)
                return Result<bool>.Fail(NotFound());

            await _repository.SoftDelete(item);

            return Result<bool>.Ok(true, "ITEM_DELETED", "Item deleted successfully");
        }

        public async Task<int> ArchiveExpired()
        {
            _logger.SetOperation("ArchiveExpiredItems");

            var archived = await _repository.ArchiveExpiredAsync(DateTime.UtcNow);

            if (archived > 0)
                _logger.Info(
                    eventName: "ITEMS_EXPIRED",
                    message: "Expired items were archived",
                    extra: new { Count = archived });

            return archived;
        }

        public Task<ItemsSnapshotDto> Snapshot() => _repository.SnapshotAsync();

        private async Task<Result<bool>> ArchiveAsync(int id, int? userId)
        {
            var archived = await _repository.TryArchiveAsync(id, userId);

            if (!archived)
                return Result<bool>.Fail(
                    new Error("ITEM_NOT_ARCHIVABLE", "Item not found or already archived", ErrorType.Conflict));

            _logger.Info(
                eventName: "ITEM_ARCHIVED",
                message: "Item archived",
                userId: _currentUser.UserIdOrNull,
                extra: new { ItemId = id, ByOwner = userId.HasValue });

            return Result<bool>.Ok(true, "ITEM_ARCHIVED", "Item archived successfully");
        }

        private async Task<Result<PagedResultDto<ItemDto>>> ListAsync(ItemListQueryDto query, int? userId)
        {
            query.Normalize();

            var search = query.Search?.Trim();
            var fromUtc = AppTime.DayStartUtc(query.From);
            var toUtc = AppTime.NextDayStartUtc(query.To);

            var options = new QueryOptions<Item>
            {
                AsNoTracking = true,
                Includes = userId.HasValue ? [] : [i => i.User!],
                Predicate = i =>
                    (userId == null || i.UserId == userId)
                    && (query.Status == null || i.Status == query.Status)
                    && (fromUtc == null || i.CreatedAt >= fromUtc)
                    && (toUtc == null || i.CreatedAt < toUtc)
                    && (string.IsNullOrEmpty(search) || i.Name.Contains(search)),
                OrderBy = q => q.OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id),
                Skip = query.Skip,
                Take = query.PageSize
            };

            var totalCount = await _repository.CountAsync(options);
            var items = await _repository.ListAsync(options);

            var page = PagedResultDto<ItemDto>.Create(items.Select(Map).ToList(), query.Page, query.PageSize, totalCount);

            return Result<PagedResultDto<ItemDto>>.Ok(page, "ITEMS_LOADED", "Items loaded successfully");
        }

        private Task<Item?> FindAsync(int id, int? userId, bool asNoTracking) =>
            _repository.FirstOrDefaultAsync(new QueryOptions<Item>
            {
                Predicate = i => i.Id == id && (userId == null || i.UserId == userId),
                Includes = userId.HasValue ? [] : [i => i.User!],
                AsNoTracking = asNoTracking
            });

        private static Error? Validate(string? name, string? description, DateTime? expiresAt)
        {
            var trimmed = (name ?? string.Empty).Trim();

            if (trimmed.Length < NameMinLength || trimmed.Length > NameMaxLength)
                return new Error(
                    "ITEM_NAME_INVALID",
                    $"Name must be between {NameMinLength} and {NameMaxLength} characters",
                    ErrorType.Validation);

            if (description?.Trim().Length > DescriptionMaxLength)
                return new Error(
                    "ITEM_DESCRIPTION_TOO_LONG",
                    $"Description must be up to {DescriptionMaxLength} characters",
                    ErrorType.Validation);

            if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
                return new Error(
                    "ITEM_EXPIRATION_IN_PAST",
                    "Expiration must be in the future",
                    ErrorType.Validation);

            return null;
        }

        private async Task<bool> NameTakenAsync(string name, int? exceptId)
        {
            var trimmed = name.Trim();
            var userId = _currentUser.UserId;

            var count = await _repository.CountAsync(new QueryOptions<Item>
            {
                Predicate = i => i.UserId == userId && i.Name == trimmed && (exceptId == null || i.Id != exceptId)
            });

            return count > 0;
        }

        private static Error NotFound() => new("ITEM_NOT_FOUND", "Item not found", ErrorType.NotFound);

        private static Error NameTaken() => new("ITEM_NAME_TAKEN", "You already have an item with this name", ErrorType.Conflict);

        private static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static ItemDto Map(Item item) => new()
        {
            Id = item.Id,
            UserId = item.UserId,
            OwnerName = item.User?.Name,
            Name = item.Name,
            Description = item.Description,
            Status = item.Status,
            ExpiresAt = item.ExpiresAt,
            ArchivedAt = item.ArchivedAt,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
