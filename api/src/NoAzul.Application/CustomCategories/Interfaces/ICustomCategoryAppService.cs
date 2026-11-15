using NoAzul.Application.CustomCategories.Dtos;
using NoAzul.Application.Pagination;
using NoAzul.Application.Response;

namespace NoAzul.Application.CustomCategories.Interfaces
{
    public interface ICustomCategoryAppService
    {
        Task<Result<PagedResultDto<CustomCategoryDto>>> GetMine(CustomCategoryListQueryDto query);
        Task<Result<CustomCategoryDto>> Create(CustomCategoryInputDto input);
        Task<Result<CustomCategoryDto>> Rename(int id, CustomCategoryInputDto input);
        Task<Result> Delete(int id);
    }
}
