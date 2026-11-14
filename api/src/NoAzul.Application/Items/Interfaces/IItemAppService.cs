using NoAzul.Application.Items.Dtos;
using NoAzul.Application.Pagination;
using NoAzul.Application.Response;

namespace NoAzul.Application.Items.Interfaces
{
    public interface IItemAppService
    {
        Task<Result<PagedResultDto<ItemDto>>> GetMine(ItemListQueryDto query);
        Task<Result<ItemDto>> GetMineById(int id);
        Task<Result<ItemDto>> Create(CreateItemInputDto input);
        Task<Result<ItemDto>> Update(int id, UpdateItemInputDto input);
        Task<Result<ItemDto>> Activate(int id);
        Task<Result<bool>> ArchiveMine(int id);

        Task<Result<PagedResultDto<ItemDto>>> GetAll(ItemListQueryDto query);
        Task<Result<ItemDto>> GetById(int id);
        Task<Result<bool>> Archive(int id);
        Task<Result<bool>> Delete(int id);

        Task<int> ArchiveExpired();
        Task<ItemsSnapshotDto> Snapshot();
    }
}
