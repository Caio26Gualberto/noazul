namespace NoAzul.Application.Transactions.Dtos
{
    public class CategoryRuleResultDto
    {
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsIgnored { get; set; }
        public int UpdatedCount { get; set; }
    }
}
