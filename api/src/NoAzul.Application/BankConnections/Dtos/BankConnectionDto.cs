namespace NoAzul.Application.BankConnections.Dtos
{
    public class BankConnectionDto
    {
        public int Id { get; set; }
        public string InstitutionName { get; set; } = string.Empty;
        public string? BankCode { get; set; }
        public string? InstitutionImageUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ProviderUpdatedAt { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<FinancialAccountDto> Accounts { get; set; } = [];
    }
}
