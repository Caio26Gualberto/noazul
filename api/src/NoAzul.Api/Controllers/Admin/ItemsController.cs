using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoAzul.Application.Items.Dtos;
using NoAzul.Application.Items.Interfaces;
using NoAzul.Domain.Constants;

namespace NoAzul.Api.Controllers.Admin
{
    [Route("api/admin/items")]
    public class ItemsController : ApiBaseController
    {
        private readonly IItemAppService _itemAppService;

        public ItemsController(IItemAppService itemAppService)
        {
            _itemAppService = itemAppService;
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Items.View)]
        public async Task<ActionResult> List([FromQuery] ItemListQueryDto query)
        {
            var result = await _itemAppService.GetAll(query);
            return ToResponse(result);
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = Permissions.Items.View)]
        public async Task<ActionResult> Get(int id)
        {
            var result = await _itemAppService.GetById(id);
            return ToResponse(result);
        }

        [HttpPost("{id:int}/archive")]
        [Authorize(Policy = Permissions.Items.Edit)]
        public async Task<ActionResult> Archive(int id)
        {
            var result = await _itemAppService.Archive(id);
            return ToResponse(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = Permissions.Items.Delete)]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _itemAppService.Delete(id);
            return ToResponse(result);
        }
    }
}
