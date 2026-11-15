namespace NoAzul.Application.Transactions.Dtos
{
    public class TransactionDto
    {
        public int Id { get; set; }
        public DateTime OccurredAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string? Bank { get; set; }
        public decimal Amount { get; set; }
        public bool IsPending { get; set; }
        public bool IsIgnored { get; set; }
        public int? InstallmentNumber { get; set; }
        public int? InstallmentTotal { get; set; }
    }
}
