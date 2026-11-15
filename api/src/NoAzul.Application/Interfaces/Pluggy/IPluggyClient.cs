namespace NoAzul.Application.Interfaces.Pluggy
{
    public interface IPluggyClient
    {
        bool IsConfigured { get; }
        Task<string> CreateConnectTokenAsync(string clientUserId, string? itemId);
        Task<PluggyItem?> GetItemAsync(string itemId);
        Task<IReadOnlyList<PluggyAccount>> ListAccountsAsync(string itemId);
        Task<IReadOnlyList<PluggyBill>> ListBillsAsync(string accountId);
        Task<IReadOnlyList<PluggyTransaction>> ListTransactionsAsync(string accountId, DateOnly from);
    }
}
