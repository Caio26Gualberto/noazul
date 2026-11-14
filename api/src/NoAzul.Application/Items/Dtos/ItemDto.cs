using NoAzul.Domain.Enums.Items;

namespace NoAzul.Application.Items.Dtos
{
    public class ItemDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? OwnerName { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public EItemStatus Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
