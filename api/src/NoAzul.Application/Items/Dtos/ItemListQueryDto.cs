using NoAzul.Application.Pagination;
using NoAzul.Domain.Enums.Items;

namespace NoAzul.Application.Items.Dtos
{
    public class ItemListQueryDto : PagedRequestDto
    {
        public string? Search { get; set; }
        public EItemStatus? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
