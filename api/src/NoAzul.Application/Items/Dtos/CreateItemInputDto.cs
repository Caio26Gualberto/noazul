namespace NoAzul.Application.Items.Dtos
{
    public class CreateItemInputDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
