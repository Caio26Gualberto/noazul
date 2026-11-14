using NoAzul.Application.Pagination;

namespace NoAzul.Application.Users.Dtos
{
    public class UserListQueryDto : PagedRequestDto
    {
        public string? Search { get; set; }
    }
}
