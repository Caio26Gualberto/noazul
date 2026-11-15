namespace NoAzul.Domain.Entities
{
    public class DescriptionAlias : EntityBase
    {
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
