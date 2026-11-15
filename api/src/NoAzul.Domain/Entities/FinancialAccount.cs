using NoAzul.Domain.Enums.BankConnections;

namespace NoAzul.Domain.Entities
{
    public class FinancialAccount : EntityBase
    {
        public int BankConnectionId { get; set; }
        public BankConnection? BankConnection { get; set; }
        public string PluggyAccountId { get; set; } = string.Empty;
        public EFinancialAccountType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string CurrencyCode { get; set; } = "BRL";
        public DateTime BalanceUpdatedAt { get; set; }
        public string? CardBrand { get; set; }
        public decimal? CreditLimit { get; set; }
        public decimal? AvailableCreditLimit { get; set; }
        public DateOnly? BillClosesOn { get; set; }
        public DateOnly? BillDueOn { get; set; }
        public decimal? PreviousBillAmount { get; set; }
    }
}
