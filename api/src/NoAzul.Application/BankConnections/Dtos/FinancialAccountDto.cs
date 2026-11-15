namespace NoAzul.Application.BankConnections.Dtos
{
    public class FinancialAccountDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public DateTime BalanceUpdatedAt { get; set; }
        public string? CardBrand { get; set; }
        public decimal? CreditLimit { get; set; }
        public decimal? AvailableCreditLimit { get; set; }
        public DateOnly? BillClosesOn { get; set; }
        public DateOnly? BillDueOn { get; set; }
        public decimal? PreviousBillAmount { get; set; }
    }
}
