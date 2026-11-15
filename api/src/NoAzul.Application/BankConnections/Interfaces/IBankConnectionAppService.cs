using NoAzul.Application.BankConnections.Dtos;
using NoAzul.Application.Pagination;
using NoAzul.Application.Response;

namespace NoAzul.Application.BankConnections.Interfaces
{
    public interface IBankConnectionAppService
    {
        Task<Result<PagedResultDto<BankConnectionDto>>> GetMine(BankConnectionListQueryDto query);
        Task<Result<ConnectTokenDto>> CreateConnectToken();
        Task<Result<BankConnectionDto>> Register(RegisterBankConnectionInputDto input);
        Task<Result<ConnectTokenDto>> CreateReconnectToken(int id);
        Task<Result> Disconnect(int id);

        Task<int> SyncAll();
        Task<bool> Sync(int bankConnectionId);
    }
}
