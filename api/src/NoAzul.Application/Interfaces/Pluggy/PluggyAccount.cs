namespace NoAzul.Application.Interfaces.Pluggy
{
    public sealed record PluggyAccount(
        string Id,
        string Type,
        string Subtype,
        string Name,
        string? MarketingName,
        string Number,
        decimal Balance,
        string? TransferNumber,
        string CurrencyCode,
        DateTime UpdatedAt,
        string? CardBrand,
        decimal? CreditLimit,
        decimal? AvailableCreditLimit,
        DateOnly? BalanceCloseDate,
        DateOnly? BalanceDueDate);
}
