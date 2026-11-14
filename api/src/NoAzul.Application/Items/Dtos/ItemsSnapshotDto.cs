namespace NoAzul.Application.Items.Dtos
{
    public class ItemsSnapshotDto
    {
        public int Draft { get; set; }
        public int Active { get; set; }
        public int Archived { get; set; }
        public DateTime TakenAt { get; set; }
    }
}
