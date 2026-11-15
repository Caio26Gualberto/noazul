using System.Globalization;
using NoAzul.Application.Abstractions.Authentication;
using NoAzul.Application.Abstractions.Logging;
using NoAzul.Application.Abstractions.Repository;
using NoAzul.Application.Common.Time;
using NoAzul.Application.Interfaces.Pluggy;
using NoAzul.Application.Pagination;
using NoAzul.Application.Response;
using NoAzul.Application.Transactions.Dtos;
using NoAzul.Application.Transactions.Interfaces;
using NoAzul.Domain.Entities;
using NoAzul.Domain.Enums.BankConnections;
using NoAzul.Domain.Enums.Transactions;

namespace NoAzul.Application.Transactions
{
    public class TransactionAppService : ITransactionAppService
    {
        private const int DescriptionMaxLength = 200;
        private const int SearchMaxLength = 100;
        private const int MaxSummaryMonths = 24;
        private const int FirstSyncMonths = 12;
        private const int ResyncDays = 30;

        private readonly ITransactionRepository _repository;
        private readonly IRepository<CategoryRule> _rules;
        private readonly IPluggyClient _pluggy;
        private readonly ICurrentUser _currentUser;
        private readonly IApplicationLogger _logger;

        public TransactionAppService(
            ITransactionRepository repository,
            IRepository<CategoryRule> rules,
            IPluggyClient pluggy,
            ICurrentUser currentUser,
            IApplicationLoggerFactory loggerFactory)
        {
            _repository = repository;
            _rules = rules;
            _pluggy = pluggy;
            _currentUser = currentUser;
            _logger = loggerFactory.Create(LogContext.Transactions);
        }

        public Task<int> RemoveForAccounts(IReadOnlyCollection<int> financialAccountIds) =>
            _repository.SoftDeleteByAccountsAsync(financialAccountIds, _currentUser.UserIdOrNull);

        public async Task<Result<PagedResultDto<TransactionDto>>> GetMine(TransactionListQueryDto query)
        {
            query.Normalize();

            var month = ParseMonth(query.Month);

            if (month == null)
                return Result<PagedResultDto<TransactionDto>>.Fail(new Error(
                    "TRANSACTION_MONTH_INVALID",
                    "Month must be in the format yyyy-MM",
                    ErrorType.Validation));

            var search = query.Search?.Trim().ToLowerInvariant();

            if (search?.Length > SearchMaxLength)
                return Result<PagedResultDto<TransactionDto>>.Fail(new Error(
                    "TRANSACTION_SEARCH_TOO_LONG",
                    $"Search must be up to {SearchMaxLength} characters",
                    ErrorType.Validation));

            var userId = _currentUser.UserId;
            var (fromUtc, toUtc) = AppTime.MonthWindowUtc(month.Value.Year, month.Value.Month);

            var options = new QueryOptions<Transaction>
            {
                AsNoTracking = true,
                Includes = [t => t.FinancialAccount!.BankConnection!],
                Predicate = t =>
                    t.UserId == userId
                    && t.OccurredAt >= fromUtc
                    && t.OccurredAt < toUtc
                    && (query.Origin == null || t.Origin == query.Origin)
                    && (query.Category == null || t.Category == query.Category)
                    && (string.IsNullOrEmpty(search) || t.Description.ToLower().Contains(search)),
                OrderBy = q => q.OrderByDescending(t => t.OccurredAt).ThenByDescending(t => t.Id),
                Skip = query.Skip,
                Take = query.PageSize
            };

            var totalCount = await _repository.CountAsync(options);
            var transactions = await _repository.ListAsync(options);

            var page = PagedResultDto<TransactionDto>.Create(
                transactions.Select(Map).ToList(), query.Page, query.PageSize, totalCount);

            return Result<PagedResultDto<TransactionDto>>.Ok(page, "TRANSACTIONS_LOADED", "Transactions loaded successfully");
        }

        public async Task<Result<List<MonthSummaryDto>>> GetMonthlySummary(int months)
        {
            if (months < 1 || months > MaxSummaryMonths)
                return Result<List<MonthSummaryDto>>.Fail(new Error(
                    "TRANSACTION_SUMMARY_MONTHS_INVALID",
                    $"Months must be between 1 and {MaxSummaryMonths}",
                    ErrorType.Validation));

            var userId = _currentUser.UserId;
            var currentMonth = new DateTime(AppTime.Today.Year, AppTime.Today.Month, 1);
            var summaries = new List<MonthSummaryDto>();

            for (var offset = months - 1; offset >= 0; offset--)
            {
                var month = currentMonth.AddMonths(-offset);
                var (fromUtc, toUtc) = AppTime.MonthWindowUtc(month.Year, month.Month);
                var totals = await _repository.SumByCategoryAsync(userId, fromUtc, toUtc);

                summaries.Add(new MonthSummaryDto
                {
                    Month = month.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                    Total = totals.Values.Sum(),
                    ByCategory = Enum.GetValues<ETransactionCategory>()
                        .ToDictionary(c => ToCamel(c.ToString()), c => totals.GetValueOrDefault(c))
                });
            }

            return Result<List<MonthSummaryDto>>.Ok(summaries, "TRANSACTION_SUMMARY_LOADED", "Monthly summary loaded successfully");
        }

        public async Task<Result<CategoryRuleResultDto>> UpdateCategory(int id, UpdateTransactionCategoryInputDto input)
        {
            _logger.SetOperation("UpdateTransactionCategory");

            var userId = _currentUser.UserId;

            var transaction = await _repository.FirstOrDefaultAsync(new QueryOptions<Transaction>
            {
                AsNoTracking = true,
                Predicate = t => t.Id == id && t.UserId == userId
            });

            if (transaction == null)
                return Result<CategoryRuleResultDto>.Fail(new Error(
                    "TRANSACTION_NOT_FOUND", "Transaction not found", ErrorType.NotFound));

            var category = transaction.Category;

            if (!input.Ignored)
            {
                if (!TryParseCategory(input.Category, out category))
                    return Result<CategoryRuleResultDto>.Fail(new Error(
                        "TRANSACTION_CATEGORY_INVALID",
                        "Category must be one of the known categories",
                        ErrorType.Validation));
            }

            var description = transaction.Description;

            var rule = await _rules.FirstOrDefaultAsync(new QueryOptions<CategoryRule>
            {
                Predicate = r => r.UserId == userId && r.Description == description
            });

            if (rule == null)
            {
                await _rules.AddAsync(new CategoryRule
                {
                    UserId = userId,
                    Description = description,
                    Category = category,
                    IsIgnored = input.Ignored
                });
            }
            else
            {
                rule.Category = category;
                rule.IsIgnored = input.Ignored;
                await _rules.UpdateAsync(rule);
            }

            var updated = await _repository.ApplyCategoryRuleAsync(userId, description, category, input.Ignored);

            _logger.Info(
                eventName: "TRANSACTION_CATEGORY_RULE_SAVED",
                message: "Category rule saved and applied to identical transactions",
                userId: userId,
                extra: new { TransactionId = id, Category = category.ToString(), input.Ignored, Updated = updated });

            return Result<CategoryRuleResultDto>.Ok(
                new CategoryRuleResultDto
                {
                    Description = description,
                    Category = ToCamel(category.ToString()),
                    IsIgnored = input.Ignored,
                    UpdatedCount = updated
                },
                input.Ignored ? "TRANSACTION_MARKED_NOT_SPENDING" : "TRANSACTION_CATEGORY_UPDATED",
                input.Ignored ? "Transaction marked as not spending" : "Transaction category updated");
        }

        public async Task<int> SyncFromProvider(FinancialAccount account, int userId)
        {
            var origin = account.Type == EFinancialAccountType.CreditCard
                ? ETransactionOrigin.Card
                : ETransactionOrigin.Account;

            var accountId = account.Id;

            var alreadySynced = await _repository.CountAsync(new QueryOptions<Transaction>
            {
                IncludeDeleted = true,
                Predicate = t => t.FinancialAccountId == accountId
            }) > 0;

            var fromDay = alreadySynced
                ? AppTime.Today.AddDays(-ResyncDays)
                : AppTime.Today.AddMonths(-FirstSyncMonths);

            var fromUtc = AppTime.ToUtc(fromDay);

            var remote = await _pluggy.ListTransactionsAsync(account.PluggyAccountId, DateOnly.FromDateTime(fromDay));

            var spending = remote
                .Where(t => t.Type == "DEBIT" && PluggyCategoryMap.IsSpending(t.CategoryId))
                .GroupBy(t => t.Id)
                .Select(g => g.First())
                .ToList();

            var remoteIds = spending.Select(t => t.Id).ToList();

            var local = await _repository.ListAsync(new QueryOptions<Transaction>
            {
                IncludeDeleted = true,
                Predicate = t => t.FinancialAccountId == accountId
                    && (t.OccurredAt >= fromUtc || remoteIds.Contains(t.PluggyTransactionId!))
            });

            var rules = (await _rules.ListAsync(new QueryOptions<CategoryRule>
            {
                AsNoTracking = true,
                Predicate = r => r.UserId == userId
            })).ToDictionary(r => r.Description);

            var localByProviderId = local
                .Where(t => t.PluggyTransactionId != null)
                .GroupBy(t => t.PluggyTransactionId!)
                .ToDictionary(g => g.Key, g => g.First());

            var toAdd = new List<Transaction>();
            var seen = new HashSet<string>();

            foreach (var item in spending)
            {
                seen.Add(item.Id);

                var occurredAt = item.Date.ToUniversalTime();
                var description = Truncate(item.Description);
                var amount = Math.Abs(item.Amount);
                rules.TryGetValue(description, out var rule);
                var category = rule?.Category ?? PluggyCategoryMap.ToCategory(item.CategoryId);
                var isIgnored = rule?.IsIgnored ?? false;
                var isPending = item.Status == "PENDING";
                var installmentNumber = item.TotalInstallments > 1 ? item.InstallmentNumber : null;
                var installmentTotal = item.TotalInstallments > 1 ? item.TotalInstallments : null;

                if (!localByProviderId.TryGetValue(item.Id, out var existing))
                {
                    toAdd.Add(new Transaction
                    {
                        UserId = userId,
                        FinancialAccountId = accountId,
                        PluggyTransactionId = item.Id,
                        Origin = origin,
                        OccurredAt = occurredAt,
                        Description = description,
                        Amount = amount,
                        Category = category,
                        ProviderCategoryId = item.CategoryId,
                        IsPending = isPending,
                        IsIgnored = isIgnored,
                        InstallmentNumber = installmentNumber,
                        InstallmentTotal = installmentTotal
                    });
                    continue;
                }

                var changed = existing.IsDeleted
                    || existing.OccurredAt != occurredAt
                    || existing.Description != description
                    || existing.Amount != amount
                    || existing.ProviderCategoryId != item.CategoryId
                    || existing.Category != category
                    || existing.IsPending != isPending
                    || existing.IsIgnored != isIgnored
                    || existing.InstallmentNumber != installmentNumber
                    || existing.InstallmentTotal != installmentTotal;

                if (!changed)
                    continue;

                existing.IsDeleted = false;
                existing.DeletedAt = null;
                existing.DeletedBy = null;
                existing.OccurredAt = occurredAt;
                existing.Description = description;
                existing.Amount = amount;
                existing.ProviderCategoryId = item.CategoryId;
                existing.Category = category;
                existing.IsPending = isPending;
                existing.IsIgnored = isIgnored;
                existing.InstallmentNumber = installmentNumber;
                existing.InstallmentTotal = installmentTotal;

                await _repository.UpdateAsync(existing);
            }

            if (toAdd.Count > 0)
                await _repository.AddRangeAsync(toAdd);

            var gone = local
                .Where(t => !t.IsDeleted
                    && t.OccurredAt >= fromUtc
                    && t.PluggyTransactionId != null
                    && !seen.Contains(t.PluggyTransactionId))
                .ToList();

            foreach (var transaction in gone)
                await _repository.SoftDelete(transaction);

            if (toAdd.Count > 0 || gone.Count > 0)
                _logger.Info(
                    eventName: "TRANSACTIONS_SYNCED",
                    message: "Transactions synced for an account",
                    userId: userId,
                    extra: new
                    {
                        FinancialAccountId = accountId,
                        FirstSync = !alreadySynced,
                        Received = remote.Count,
                        Added = toAdd.Count,
                        Removed = gone.Count
                    });

            return toAdd.Count;
        }

        private static (int Year, int Month)? ParseMonth(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return (AppTime.Today.Year, AppTime.Today.Month);

            return DateTime.TryParseExact(value.Trim(), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? (parsed.Year, parsed.Month)
                : null;
        }

        private static bool TryParseCategory(string? value, out ETransactionCategory category)
        {
            category = ETransactionCategory.Other;
            return !string.IsNullOrWhiteSpace(value)
                && !int.TryParse(value, out _)
                && Enum.TryParse(value.Trim(), ignoreCase: true, out category)
                && Enum.IsDefined(category);
        }

        private static string Truncate(string value)
        {
            var trimmed = value.Trim();
            return trimmed.Length <= DescriptionMaxLength ? trimmed : trimmed[..DescriptionMaxLength];
        }

        private static string ToCamel(string value) =>
            string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];

        private static TransactionDto Map(Transaction transaction) => new()
        {
            Id = transaction.Id,
            OccurredAt = transaction.OccurredAt,
            Description = transaction.Description,
            Category = ToCamel(transaction.Category.ToString()),
            Origin = ToCamel(transaction.Origin.ToString()),
            Bank = transaction.FinancialAccount?.BankConnection?.InstitutionName,
            Amount = transaction.Amount,
            IsPending = transaction.IsPending,
            IsIgnored = transaction.IsIgnored,
            InstallmentNumber = transaction.InstallmentNumber,
            InstallmentTotal = transaction.InstallmentTotal
        };
    }
}
