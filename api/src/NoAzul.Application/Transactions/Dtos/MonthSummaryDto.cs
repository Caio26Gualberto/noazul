namespace NoAzul.Application.Transactions.Dtos
{
    public class MonthSummaryDto
    {
        public string Month { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public Dictionary<string, decimal> ByCategory { get; set; } = [];
    }
}
