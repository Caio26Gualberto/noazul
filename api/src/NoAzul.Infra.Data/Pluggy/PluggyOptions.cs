namespace NoAzul.Infra.Data.Pluggy
{
    public class PluggyOptions
    {
        public const string SectionName = "Pluggy";
        public const string DefaultBaseUrl = "https://api.pluggy.ai";

        public string BaseUrl { get; set; } = DefaultBaseUrl;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
    }
}
