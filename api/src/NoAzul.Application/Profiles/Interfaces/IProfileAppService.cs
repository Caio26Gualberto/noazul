using NoAzul.Application.Profiles.Dtos;
using NoAzul.Application.Response;

namespace NoAzul.Application.Profiles.Interfaces
{
    public interface IProfileAppService
    {
        Task<Result<ProfileDto>> Me();
        Task<Result<ProfileDto>> UpdateName(UpdateProfileNameDto input);
    }
}
