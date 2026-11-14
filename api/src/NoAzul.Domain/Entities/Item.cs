using NoAzul.Domain.Enums.Items;

namespace NoAzul.Domain.Entities
{
    public class Item : EntityBase
    {
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public EItemStatus Status { get; set; } = EItemStatus.Draft;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? ArchivedAt { get; set; }
    }
}
