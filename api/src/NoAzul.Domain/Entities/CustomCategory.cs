namespace NoAzul.Domain.Entities
{
    public class CustomCategory : EntityBase
    {
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
