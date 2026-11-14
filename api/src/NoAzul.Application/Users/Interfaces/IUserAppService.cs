using NoAzul.Application.Pagination;
using NoAzul.Application.Response;
using NoAzul.Application.Users.Dtos;

namespace NoAzul.Application.Users.Interfaces
{
    public interface IUserAppService
    {
        Task<Result<PagedResultDto<UserDto>>> GetAll(UserListQueryDto query);
        Task<Result<UserDto>> Get(int userId);
        Task<Result<InvitedUserDto>> Invite(InviteUserInputDto input);
        Task<Result> RevokeAccess(int userId);
    }
}
