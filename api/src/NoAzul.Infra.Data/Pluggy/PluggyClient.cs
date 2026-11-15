using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using NoAzul.Application.Interfaces.Pluggy;
using NoAzul.Infra.Data.Cache;

namespace NoAzul.Infra.Data.Pluggy
{
    public class PluggyClient : IPluggyClient
    {
        private const string ApiKeyCacheKey = "pluggy:api-key";
        private const int PageSize = 100;
        private static readonly TimeSpan ApiKeyTtl = TimeSpan.FromMinutes(100);

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _http;
        private readonly ICacheClient _cache;
        private readonly PluggyOptions _options;

        public PluggyClient(HttpClient http, ICacheClient cache, IOptions<PluggyOptions> options)
        {
            _http = http;
            _cache = cache;
            _options = options.Value;
        }

        public bool IsConfigured => _options.IsConfigured;

        public async Task<string> CreateConnectTokenAsync(string clientUserId, string? itemId)
        {
            var body = new ConnectTokenRequest(itemId, new ConnectTokenOptions(clientUserId, AvoidDuplicates: true));
            using var request = await AuthorizedAsync(HttpMethod.Post, "connect_token");
            request.Content = JsonContent.Create(body, options: JsonOptions);

            var response = await SendAsync<ConnectTokenResponse>(request);
            return response.AccessToken;
        }

        public async Task<PluggyItem?> GetItemAsync(string itemId)
        {
            using var request = await AuthorizedAsync(HttpMethod.Get, $"items/{Uri.EscapeDataString(itemId)}");
            using var response = await _http.SendAsync(request);

            if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
                return null;

            await EnsureSuccessAsync(response);

            var item = await response.Content.ReadFromJsonAsync<ItemResponse>(JsonOptions)
                ?? throw new InvalidOperationException("A Pluggy devolveu um item vazio.");

            return new PluggyItem(
                item.Id,
                item.Status,
                item.ExecutionStatus,
                item.Connector?.Name,
                item.Connector?.ImageUrl,
                item.LastUpdatedAt);
        }

        public async Task<IReadOnlyList<PluggyAccount>> ListAccountsAsync(string itemId)
        {
            var accounts = await ListAllAsync<AccountResponse>($"accounts?itemId={Uri.EscapeDataString(itemId)}");

            return accounts.Select(a => new PluggyAccount(
                a.Id,
                a.Type,
                a.Subtype,
                a.Name,
                a.MarketingName,
                a.Number,
                a.Balance,
                a.BankData?.TransferNumber,
                a.CurrencyCode,
                a.UpdatedAt,
                a.CreditData?.Brand,
                a.CreditData?.CreditLimit,
                a.CreditData?.AvailableCreditLimit,
                ToDate(a.CreditData?.BalanceCloseDate),
                ToDate(a.CreditData?.BalanceDueDate))).ToList();
        }

        public async Task<IReadOnlyList<PluggyBill>> ListBillsAsync(string accountId)
        {
            var bills = await ListAllAsync<BillResponse>($"bills?accountId={Uri.EscapeDataString(accountId)}");

            return bills.Select(b => new PluggyBill(b.Id, DateOnly.FromDateTime(b.DueDate), b.TotalAmount)).ToList();
        }

        private async Task<List<T>> ListAllAsync<T>(string path)
        {
            var all = new List<T>();
            var page = 1;

            while (true)
            {
                using var request = await AuthorizedAsync(HttpMethod.Get, $"{path}&page={page}&pageSize={PageSize}");
                var response = await SendAsync<PageResponse<T>>(request);

                all.AddRange(response.Results ?? []);

                if (page >= response.TotalPages || response.Results is not { Count: > 0 })
                    return all;

                page++;
            }
        }

        private async Task<HttpRequestMessage> AuthorizedAsync(HttpMethod method, string path)
        {
            if (!IsConfigured)
                throw new InvalidOperationException("Pluggy não configurada. Defina Pluggy:ClientId e Pluggy:ClientSecret.");

            var apiKey = await _cache.GetOrCreateAsync(
                ApiKeyCacheKey,
                _ => FetchApiKeyAsync(),
                new CacheEntryOptions(ApiKeyTtl));

            var request = new HttpRequestMessage(method, path);
            request.Headers.Add("X-API-KEY", apiKey);
            return request;
        }

        private async Task<string> FetchApiKeyAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "auth")
            {
                Content = JsonContent.Create(new AuthRequest(_options.ClientId, _options.ClientSecret), options: JsonOptions)
            };

            var response = await SendAsync<AuthResponse>(request);
            return response.ApiKey;
        }

        private async Task<T> SendAsync<T>(HttpRequestMessage request)
        {
            using var response = await _http.SendAsync(request);
            await EnsureSuccessAsync(response);

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions)
                ?? throw new InvalidOperationException($"A Pluggy devolveu corpo vazio em {request.RequestUri?.AbsolutePath}.");
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var code = TryReadCode(await response.Content.ReadAsStringAsync());
            var request = response.RequestMessage;

            throw new HttpRequestException(
                $"A Pluggy respondeu {(int)response.StatusCode} em {request?.Method} {request?.RequestUri?.AbsolutePath}"
                + (code == null ? "." : $" ({code})."),
                inner: null,
                statusCode: response.StatusCode);
        }

        private static string? TryReadCode(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.TryGetProperty("codeDescription", out var code) ? code.GetString() : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static DateOnly? ToDate(DateTime? value) =>
            value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

        private sealed record AuthRequest(string ClientId, string ClientSecret);

        private sealed record AuthResponse(string ApiKey);

        private sealed record ConnectTokenRequest(
            [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ItemId,
            ConnectTokenOptions Options);

        private sealed record ConnectTokenOptions(string ClientUserId, bool AvoidDuplicates);

        private sealed record ConnectTokenResponse(string AccessToken);

        private sealed record PageResponse<T>(int TotalPages, List<T>? Results);

        private sealed record ConnectorResponse(string? Name, string? ImageUrl);

        private sealed record ItemResponse(
            string Id,
            string Status,
            string? ExecutionStatus,
            ConnectorResponse? Connector,
            DateTime? LastUpdatedAt);

        private sealed record CreditDataResponse(
            string? Brand,
            decimal? CreditLimit,
            decimal? AvailableCreditLimit,
            DateTime? BalanceCloseDate,
            DateTime? BalanceDueDate);

        private sealed record AccountResponse(
            string Id,
            string Type,
            string Subtype,
            string Name,
            string? MarketingName,
            string Number,
            decimal Balance,
            string CurrencyCode,
            DateTime UpdatedAt,
            BankDataResponse? BankData,
            CreditDataResponse? CreditData);

        private sealed record BankDataResponse(string? TransferNumber);

        private sealed record BillResponse(string Id, DateTime DueDate, decimal TotalAmount);
    }
}
