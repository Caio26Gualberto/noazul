namespace NoAzul.Application.Interfaces.Pluggy
{
    public sealed record PluggyBill(string Id, DateOnly DueDate, decimal TotalAmount);
}
