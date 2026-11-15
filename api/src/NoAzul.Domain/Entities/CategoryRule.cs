using NoAzul.Domain.Enums.Transactions;

namespace NoAzul.Domain.Entities
{
    public class CategoryRule : EntityBase
    {
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Description { get; set; } = string.Empty;
        public ETransactionCategory Category { get; set; }
        public bool IsIgnored { get; set; }
    }
}
