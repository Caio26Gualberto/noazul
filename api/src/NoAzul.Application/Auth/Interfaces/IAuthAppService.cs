using NoAzul.Application.Auth.Dtos;
using NoAzul.Application.Response;

namespace NoAzul.Application.Auth.Interfaces
{
    public interface IAuthAppService
    {
        Task<Result<LoginResponseDto>> Login(LoginInputDto input);
        Result<RegistrationStatusDto> GetRegistrationStatus();
        Task<Result<LoginResponseDto>> Register(RegisterInputDto input);
        Task<Result<TokensDto>> RefreshTokens(RefreshTokenRequestDto input);
        Task<Result<bool>> Logout();
        Task<Result<TokensDto>> ChangePassword(ChangePasswordInputDto input);
        Task<Result<TokensDto>> SetInitialPassword(SetInitialPasswordInputDto input);
    }
}
