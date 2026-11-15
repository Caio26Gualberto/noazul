using NoAzul.Application.Abstractions.Authentication;
using NoAzul.Application.Abstractions.Logging;
using NoAzul.Application.Abstractions.Repository;
using NoAzul.Application.Abstractions.Repository.UnitOfWork;
using NoAzul.Application.CustomCategories.Dtos;
using NoAzul.Application.CustomCategories.Interfaces;
using NoAzul.Application.Pagination;
using NoAzul.Application.Response;
using NoAzul.Application.Transactions;
using NoAzul.Application.Transactions.Interfaces;
using NoAzul.Domain.Entities;

namespace NoAzul.Application.CustomCategories
{
    public class CustomCategoryAppService : ICustomCategoryAppService
    {
        private const int NameMaxLength = 40;
        private const int MaxPerUser = 50;

        private readonly IRepository<CustomCategory> _repository;
        private readonly ITransactionAppService _transactions;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly IApplicationLogger _logger;

        public CustomCategoryAppService(
            IRepository<CustomCategory> repository,
            ITransactionAppService transactions,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            IApplicationLoggerFactory loggerFactory)
        {
            _repository = repository;
            _transactions = transactions;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = loggerFactory.Create(LogContext.CustomCategories);
        }

        public async Task<Result<PagedResultDto<CustomCategoryDto>>> GetMine(CustomCategoryListQueryDto query)
        {
            query.Normalize();

            var userId = _currentUser.UserId;

            var options = new QueryOptions<CustomCategory>
            {
                AsNoTracking = true,
                Predicate = c => c.UserId == userId,
                OrderBy = q => q.OrderBy(c => c.Name).ThenBy(c => c.Id),
                Skip = query.Skip,
                Take = query.PageSize
            };

            var totalCount = await _repository.CountAsync(options);
            var categories = await _repository.ListAsync(options);

            var page = PagedResultDto<CustomCategoryDto>.Create(
                categories.Select(Map).ToList(), query.Page, query.PageSize, totalCount);

            return Result<PagedResultDto<CustomCategoryDto>>.Ok(
                page, "CUSTOM_CATEGORIES_LOADED", "Custom categories loaded successfully");
        }

        public async Task<Result<CustomCategoryDto>> Create(CustomCategoryInputDto input)
        {
            _logger.SetOperation("CreateCustomCategory");

            var name = NormalizeName(input.Name);

            if (name == null)
                return Result<CustomCategoryDto>.Fail(NameInvalid());

            var userId = _currentUser.UserId;

            var count = await _repository.CountAsync(new QueryOptions<CustomCategory>
            {
                Predicate = c => c.UserId == userId
            });

            if (count >= MaxPerUser)
                return Result<CustomCategoryDto>.Fail(new Error(
                    "CUSTOM_CATEGORY_LIMIT_REACHED",
                    $"You can have up to {MaxPerUser} categories of your own",
                    ErrorType.Validation));

            if (await NameTakenAsync(userId, name, exceptId: null))
                return Result<CustomCategoryDto>.Fail(NameTaken());

            var category = new CustomCategory { UserId = userId, Name = name };
            await _repository.AddAsync(category);

            _logger.Info(
                eventName: "CUSTOM_CATEGORY_CREATED",
                message: "Custom category created",
                userId: userId,
                extra: new { CustomCategoryId = category.Id });

            return Result<CustomCategoryDto>.Ok(Map(category), "CUSTOM_CATEGORY_CREATED", "Category created successfully");
        }

        public async Task<Result<CustomCategoryDto>> Rename(int id, CustomCategoryInputDto input)
        {
            _logger.SetOperation("RenameCustomCategory");
            _logger.Set("CustomCategoryId", id);

            var name = NormalizeName(input.Name);

            if (name == null)
                return Result<CustomCategoryDto>.Fail(NameInvalid());

            var userId = _currentUser.UserId;
            var category = await FindMineAsync(id, userId);

            if (category == null)
                return Result<CustomCategoryDto>.Fail(NotFound());

            if (await NameTakenAsync(userId, name, exceptId: id))
                return Result<CustomCategoryDto>.Fail(NameTaken());

            category.Name = name;
            await _repository.UpdateAsync(category);

            return Result<CustomCategoryDto>.Ok(Map(category), "CUSTOM_CATEGORY_RENAMED", "Category renamed successfully");
        }

        public async Task<Result> Delete(int id)
        {
            _logger.SetOperation("DeleteCustomCategory");
            _logger.Set("CustomCategoryId", id);

            var userId = _currentUser.UserId;
            var category = await FindMineAsync(id, userId);

            if (category == null)
                return Result.Fail(NotFound());

            int restored;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                restored = await _transactions.ClearCustomCategory(id);
                await _repository.SoftDelete(category);
                await _unitOfWork.CommitAsync();
            }
            catch
            {
                await _unitOfWork.RollbackAsync();
                throw;
            }

            _logger.Info(
                eventName: "CUSTOM_CATEGORY_DELETED",
                message: "Custom category deleted; its transactions went back to the automatic category",
                userId: userId,
                extra: new { CustomCategoryId = id, Restored = restored });

            return Result.Ok("CUSTOM_CATEGORY_DELETED", "Category deleted successfully");
        }

        private Task<CustomCategory?> FindMineAsync(int id, int userId) =>
            _repository.FirstOrDefaultAsync(new QueryOptions<CustomCategory>
            {
                Predicate = c => c.Id == id && c.UserId == userId
            });

        private async Task<bool> NameTakenAsync(int userId, string name, int? exceptId)
        {
            var lowered = name.ToLower();

            return await _repository.CountAsync(new QueryOptions<CustomCategory>
            {
                Predicate = c => c.UserId == userId
                    && c.Name.ToLower() == lowered
                    && (exceptId == null || c.Id != exceptId)
            }) > 0;
        }

        private static string? NormalizeName(string? value)
        {
            var name = string.Join(' ', (value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));
            return name.Length is 0 or > NameMaxLength ? null : name;
        }

        private static Error NameInvalid() =>
            new("CUSTOM_CATEGORY_NAME_INVALID", $"Name must be between 1 and {NameMaxLength} characters", ErrorType.Validation);

        private static Error NameTaken() =>
            new("CUSTOM_CATEGORY_NAME_TAKEN", "You already have a category with this name", ErrorType.Conflict);

        private static Error NotFound() =>
            new("CUSTOM_CATEGORY_NOT_FOUND", "Category not found", ErrorType.NotFound);

        private static CustomCategoryDto Map(CustomCategory category) => new()
        {
            Id = category.Id,
            Key = CategoryKeys.Custom(category.Id),
            Name = category.Name
        };
    }
}
