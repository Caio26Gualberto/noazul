namespace NoAzul.Application.Interfaces.Pluggy
{
    public sealed record PluggyTransaction(
        string Id,
        string Description,
        decimal Amount,
        DateTime Date,
        string? Type,
        string? Status,
        string? CategoryId,
        int? InstallmentNumber,
        int? TotalInstallments);
}
