using NoAzul.Domain.Enums.BankConnections;

namespace NoAzul.Domain.Entities
{
    public class BankConnection : EntityBase
    {
        public int UserId { get; set; }
        public User? User { get; set; }
        public string PluggyItemId { get; set; } = string.Empty;
        public string InstitutionName { get; set; } = string.Empty;
        public string? BankCode { get; set; }
        public string? InstitutionImageUrl { get; set; }
        public EBankConnectionStatus Status { get; set; } = EBankConnectionStatus.Updating;
        public DateTime? ProviderUpdatedAt { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public List<FinancialAccount> Accounts { get; set; } = [];
    }
}
