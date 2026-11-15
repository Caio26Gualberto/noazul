namespace NoAzul.Application.BankConnections.Dtos
{
    public class ConnectTokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? ItemId { get; set; }
    }
}
