using NoAzul.Domain.Enums.Transactions;

namespace NoAzul.Domain.Entities
{
    public class Transaction : EntityBase
    {
        public int UserId { get; set; }
        public User? User { get; set; }
        public int? FinancialAccountId { get; set; }
        public FinancialAccount? FinancialAccount { get; set; }
        public string? PluggyTransactionId { get; set; }
        public ETransactionOrigin Origin { get; set; }
        public DateTime OccurredAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public ETransactionCategory Category { get; set; } = ETransactionCategory.Other;
        public int? CustomCategoryId { get; set; }
        public CustomCategory? CustomCategory { get; set; }
        public string? ProviderCategoryId { get; set; }
        public bool IsPending { get; set; }
        public bool IsIgnored { get; set; }
        public int? InstallmentNumber { get; set; }
        public int? InstallmentTotal { get; set; }
    }
}
