using NoAzul.Application.Abstractions.Authentication;
using NoAzul.Application.Abstractions.Jobs;
using NoAzul.Application.Abstractions.Logging;
using NoAzul.Application.Abstractions.Repository;
using NoAzul.Application.BankConnections.Dtos;
using NoAzul.Application.BankConnections.Interfaces;
using NoAzul.Application.Common.Time;
using NoAzul.Application.Interfaces.Pluggy;
using NoAzul.Application.Pagination;
using NoAzul.Application.Response;
using NoAzul.Domain.Entities;
using NoAzul.Domain.Enums.BankConnections;

namespace NoAzul.Application.BankConnections
{
    public class BankConnectionAppService : IBankConnectionAppService
    {
        private const int ItemIdMaxLength = 64;
        private const int InstitutionNameMaxLength = 120;
        private const int ImageUrlMaxLength = 500;

        private readonly IRepository<BankConnection> _connections;
        private readonly IRepository<FinancialAccount> _accounts;
        private readonly IPluggyClient _pluggy;
        private readonly IEnqueuedJobDispatcher _dispatcher;
        private readonly ICurrentUser _currentUser;
        private readonly IApplicationLogger _logger;

        public BankConnectionAppService(
            IRepository<BankConnection> connections,
            IRepository<FinancialAccount> accounts,
            IPluggyClient pluggy,
            IEnqueuedJobDispatcher dispatcher,
            ICurrentUser currentUser,
            IApplicationLoggerFactory loggerFactory)
        {
            _connections = connections;
            _accounts = accounts;
            _pluggy = pluggy;
            _dispatcher = dispatcher;
            _currentUser = currentUser;
            _logger = loggerFactory.Create(LogContext.BankConnections);
        }

        public async Task<Result<PagedResultDto<BankConnectionDto>>> GetMine(BankConnectionListQueryDto query)
        {
            query.Normalize();

            var userId = _currentUser.UserId;

            var options = new QueryOptions<BankConnection>
            {
                AsNoTracking = true,
                Predicate = c => c.UserId == userId,
                OrderBy = q => q.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id),
                Skip = query.Skip,
                Take = query.PageSize
            };

            var totalCount = await _connections.CountAsync(options);
            var connections = await _connections.ListAsync(options);

            var connectionIds = connections.Select(c => c.Id).ToList();
            var accounts = connectionIds.Count == 0
                ? []
                : await _accounts.ListAsync(new QueryOptions<FinancialAccount>
                {
                    AsNoTracking = true,
                    Predicate = a => connectionIds.Contains(a.BankConnectionId),
                    OrderBy = q => q.OrderBy(a => a.Type).ThenBy(a => a.Id)
                });

            var items = connections
                .Select(c => Map(c, accounts.Where(a => a.BankConnectionId == c.Id)))
                .ToList();

            var page = PagedResultDto<BankConnectionDto>.Create(items, query.Page, query.PageSize, totalCount);

            return Result<PagedResultDto<BankConnectionDto>>.Ok(
                page, "BANK_CONNECTIONS_LOADED", "Bank connections loaded successfully");
        }

        public async Task<Result<ConnectTokenDto>> CreateConnectToken()
        {
            _logger.SetOperation("CreateConnectToken");

            if (!_pluggy.IsConfigured)
                return Result<ConnectTokenDto>.Fail(NotConfigured());

            try
            {
                var token = await _pluggy.CreateConnectTokenAsync(_currentUser.UserId.ToString(), itemId: null);

                return Result<ConnectTokenDto>.Ok(
                    new ConnectTokenDto { AccessToken = token },
                    "BANK_CONNECT_TOKEN_CREATED",
                    "Connect token created successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(
                    eventName: "PLUGGY_CONNECT_TOKEN_FAILED",
                    message: "Could not create a Pluggy connect token",
                    exception: ex,
                    userId: _currentUser.UserIdOrNull);

                return Result<ConnectTokenDto>.Fail(ProviderUnavailable());
            }
        }

        public async Task<Result<BankConnectionDto>> Register(RegisterBankConnectionInputDto input)
        {
            _logger.SetOperation("RegisterBankConnection");

            var itemId = (input.ItemId ?? string.Empty).Trim();

            if (itemId.Length == 0 || itemId.Length > ItemIdMaxLength)
                return Result<BankConnectionDto>.Fail(new Error(
                    "BANK_CONNECTION_ITEM_ID_INVALID",
                    $"Item id must be between 1 and {ItemIdMaxLength} characters",
                    ErrorType.Validation));

            if (!_pluggy.IsConfigured)
                return Result<BankConnectionDto>.Fail(NotConfigured());

            _logger.Set("PluggyItemId", itemId);

            var userId = _currentUser.UserId;

            var existing = await _connections.FirstOrDefaultAsync(new QueryOptions<BankConnection>
            {
                Predicate = c => c.PluggyItemId == itemId
            });

            if (existing != null && existing.UserId != userId)
                return Result<BankConnectionDto>.Fail(new Error(
                    "BANK_CONNECTION_TAKEN",
                    "This bank connection belongs to another account",
                    ErrorType.Conflict));

            if (existing != null)
            {
                EnqueueSync(existing.Id, userId);
                return Result<BankConnectionDto>.Ok(
                    Map(existing, []), "BANK_CONNECTION_REFRESHED", "Bank connection already exists and will be refreshed");
            }

            PluggyItem? item;

            try
            {
                item = await _pluggy.GetItemAsync(itemId);
            }
            catch (Exception ex)
            {
                _logger.Error(
                    eventName: "PLUGGY_ITEM_LOOKUP_FAILED",
                    message: "Could not read the Pluggy item while registering a connection",
                    exception: ex,
                    userId: userId);

                return Result<BankConnectionDto>.Fail(ProviderUnavailable());
            }

            if (item == null)
                return Result<BankConnectionDto>.Fail(new Error(
                    "BANK_CONNECTION_ITEM_NOT_FOUND",
                    "Pluggy did not find this connection",
                    ErrorType.Validation));

            var connection = new BankConnection
            {
                UserId = userId,
                PluggyItemId = item.Id,
                InstitutionName = Truncate(item.ConnectorName, InstitutionNameMaxLength) ?? "Banco",
                InstitutionImageUrl = InstitutionImage(item),
                Status = MapStatus(item.Status),
                ProviderUpdatedAt = ToUtc(item.LastUpdatedAt)
            };

            await _connections.AddAsync(connection);

            _logger.Set("BankConnectionId", connection.Id);
            _logger.Info(
                eventName: "BANK_CONNECTION_CREATED",
                message: "Bank connection created",
                userId: userId,
                extra: new { BankConnectionId = connection.Id, Institution = connection.InstitutionName, item.Status });

            EnqueueSync(connection.Id, userId);

            return Result<BankConnectionDto>.Ok(Map(connection, []), "BANK_CONNECTION_CREATED", "Bank connection created successfully");
        }

        public async Task<int> SyncAll()
        {
            _logger.SetOperation("SyncAllBankConnections");

            if (!_pluggy.IsConfigured)
            {
                _logger.Warning(
                    eventName: "PLUGGY_NOT_CONFIGURED",
                    message: "Bank connections were not synced because Pluggy credentials are missing");
                return 0;
            }

            var connections = await _connections.ListAsync(new QueryOptions<BankConnection>
            {
                OrderBy = q => q.OrderBy(c => c.Id)
            });

            var synced = 0;

            foreach (var connection in connections)
            {
                try
                {
                    await SyncConnectionAsync(connection);
                    synced++;
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        eventName: "BANK_CONNECTION_SYNC_FAILED",
                        message: "Bank connection sync failed; the others continue",
                        exception: ex,
                        userId: connection.UserId,
                        extra: new { BankConnectionId = connection.Id });
                }
            }

            _logger.Info(
                eventName: "BANK_CONNECTIONS_SYNCED",
                message: "Daily bank connection sync finished",
                extra: new { Total = connections.Count, Synced = synced });

            return synced;
        }

        public async Task<bool> Sync(int bankConnectionId)
        {
            _logger.SetOperation("SyncBankConnection");
            _logger.Set("BankConnectionId", bankConnectionId);

            if (!_pluggy.IsConfigured)
                return false;

            var connection = await _connections.FirstOrDefaultAsync(new QueryOptions<BankConnection>
            {
                Predicate = c => c.Id == bankConnectionId
            });

            if (connection == null)
                return false;

            await SyncConnectionAsync(connection);
            return true;
        }

        private async Task SyncConnectionAsync(BankConnection connection)
        {
            var item = await _pluggy.GetItemAsync(connection.PluggyItemId);

            if (item == null)
            {
                connection.Status = EBankConnectionStatus.NeedsReconnect;
                connection.LastSyncedAt = DateTime.UtcNow;
                await _connections.UpdateAsync(connection);

                _logger.Warning(
                    eventName: "BANK_CONNECTION_ITEM_GONE",
                    message: "Pluggy no longer has this item; the connection needs to be reconnected",
                    userId: connection.UserId,
                    extra: new { BankConnectionId = connection.Id });
                return;
            }

            var remoteAccounts = await _pluggy.ListAccountsAsync(connection.PluggyItemId);

            var localAccounts = await _accounts.ListAsync(new QueryOptions<FinancialAccount>
            {
                IncludeDeleted = true,
                Predicate = a => a.BankConnectionId == connection.Id
            });

            var seen = new HashSet<string>();

            foreach (var remote in remoteAccounts)
            {
                var type = MapAccountType(remote.Subtype);

                if (type == null)
                    continue;

                seen.Add(remote.Id);

                var previousBill = type == EFinancialAccountType.CreditCard
                    ? await PreviousBillAmountAsync(remote.Id)
                    : null;

                var local = localAccounts.FirstOrDefault(a => a.PluggyAccountId == remote.Id);
                var isNew = local == null;

                local ??= new FinancialAccount { BankConnectionId = connection.Id, PluggyAccountId = remote.Id };

                local.Type = type.Value;
                local.Name = Truncate(remote.MarketingName ?? remote.Name, InstitutionNameMaxLength) ?? string.Empty;
                local.Number = Truncate(remote.Number, ItemIdMaxLength) ?? string.Empty;
                local.Balance = remote.Balance;
                local.CurrencyCode = remote.CurrencyCode;
                local.BalanceUpdatedAt = ToUtc(remote.UpdatedAt) ?? DateTime.UtcNow;
                local.CardBrand = remote.CardBrand?.Trim().ToLowerInvariant();
                local.CreditLimit = remote.CreditLimit;
                local.AvailableCreditLimit = remote.AvailableCreditLimit;
                local.BillClosesOn = remote.BalanceCloseDate;
                local.BillDueOn = remote.BalanceDueDate;
                local.PreviousBillAmount = previousBill;

                if (isNew)
                {
                    await _accounts.AddAsync(local);
                    continue;
                }

                if (local.IsDeleted)
                {
                    local.IsDeleted = false;
                    local.DeletedAt = null;
                    local.DeletedBy = null;
                }

                await _accounts.UpdateAsync(local);
            }

            foreach (var gone in localAccounts.Where(a => !a.IsDeleted && !seen.Contains(a.PluggyAccountId)))
                await _accounts.SoftDelete(gone);

            var bank = remoteAccounts
                .Select(a => BankCodes.FromTransferNumber(a.TransferNumber))
                .FirstOrDefault(found => found != null);

            if (bank != null)
            {
                connection.BankCode = bank.Value.Code;
                connection.InstitutionName = bank.Value.Name;
            }

            connection.Status = MapStatus(item.Status);
            connection.ProviderUpdatedAt = ToUtc(item.LastUpdatedAt);
            connection.LastSyncedAt = DateTime.UtcNow;
            connection.InstitutionImageUrl = InstitutionImage(item);

            await _connections.UpdateAsync(connection);
        }

        private async Task<decimal?> PreviousBillAmountAsync(string accountId)
        {
            var today = DateOnly.FromDateTime(AppTime.Today);
            var bills = await _pluggy.ListBillsAsync(accountId);

            return bills
                .Where(b => b.DueDate <= today)
                .OrderByDescending(b => b.DueDate)
                .Select(b => (decimal?)b.TotalAmount)
                .FirstOrDefault();
        }

        private void EnqueueSync(int bankConnectionId, int userId)
        {
            try
            {
                _dispatcher.EnqueueBankConnectionSync(bankConnectionId);
            }
            catch (Exception ex)
            {
                _logger.Warning(
                    eventName: "BANK_CONNECTION_SYNC_NOT_ENQUEUED",
                    message: "Bank connection was saved but the first sync could not be enqueued",
                    userId: userId,
                    extra: new { BankConnectionId = bankConnectionId, Error = ex.Message });
            }
        }

        private static EBankConnectionStatus MapStatus(string status) => status switch
        {
            "UPDATED" => EBankConnectionStatus.Ok,
            "UPDATING" => EBankConnectionStatus.Updating,
            "LOGIN_ERROR" or "WAITING_USER_INPUT" => EBankConnectionStatus.NeedsReconnect,
            _ => EBankConnectionStatus.Failed
        };

        private static EFinancialAccountType? MapAccountType(string subtype) => subtype switch
        {
            "CHECKING_ACCOUNT" => EFinancialAccountType.Checking,
            "SAVINGS_ACCOUNT" => EFinancialAccountType.Savings,
            "CREDIT_CARD" => EFinancialAccountType.CreditCard,
            _ => null
        };

        private static string? InstitutionImage(PluggyItem item) =>
            item.ConnectorName == BankCodes.MeuPluggyConnectorName
                ? null
                : Truncate(item.ConnectorImageUrl, ImageUrlMaxLength);

        private static DateTime? ToUtc(DateTime? value) =>
            value.HasValue ? value.Value.ToUniversalTime() : null;

        private static string? Truncate(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();
            return trimmed.Length <= max ? trimmed : trimmed[..max];
        }

        private static Error NotConfigured() =>
            new("PLUGGY_NOT_CONFIGURED", "Bank integration is not configured on the server", ErrorType.Unexpected);

        private static Error ProviderUnavailable() =>
            new("PLUGGY_UNAVAILABLE", "Pluggy could not be reached; try again in a few minutes", ErrorType.Unexpected);

        private static BankConnectionDto Map(BankConnection connection, IEnumerable<FinancialAccount> accounts) => new()
        {
            Id = connection.Id,
            InstitutionName = connection.InstitutionName,
            BankCode = connection.BankCode,
            InstitutionImageUrl = connection.InstitutionImageUrl,
            Status = ToCamel(connection.Status.ToString()),
            ProviderUpdatedAt = connection.ProviderUpdatedAt,
            LastSyncedAt = connection.LastSyncedAt,
            CreatedAt = connection.CreatedAt,
            Accounts = accounts.Select(Map).ToList()
        };

        private static FinancialAccountDto Map(FinancialAccount account) => new()
        {
            Id = account.Id,
            Type = ToCamel(account.Type.ToString()),
            Name = account.Name,
            Number = account.Number,
            Balance = account.Balance,
            CurrencyCode = account.CurrencyCode,
            BalanceUpdatedAt = account.BalanceUpdatedAt,
            CardBrand = account.CardBrand,
            CreditLimit = account.CreditLimit,
            AvailableCreditLimit = account.AvailableCreditLimit,
            BillClosesOn = account.BillClosesOn,
            BillDueOn = account.BillDueOn,
            PreviousBillAmount = account.PreviousBillAmount
        };

        private static string ToCamel(string value) =>
            string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
    }
}
