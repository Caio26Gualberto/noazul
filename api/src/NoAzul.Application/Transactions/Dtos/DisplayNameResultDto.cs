namespace NoAzul.Application.Transactions.Dtos
{
    public class DisplayNameResultDto
    {
        public string Description { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public int AffectedCount { get; set; }
    }
}
