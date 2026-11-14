using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoAzul.Api.RateLimiting;
using NoAzul.Application.Items.Dtos;
using NoAzul.Application.Items.Interfaces;

namespace NoAzul.Api.Controllers
{
    [Route("api/items")]
    public class ItemsController : ApiBaseController
    {
        private readonly IItemAppService _itemAppService;

        public ItemsController(IItemAppService itemAppService)
        {
            _itemAppService = itemAppService;
        }

        [HttpGet]
        public async Task<ActionResult> List([FromQuery] ItemListQueryDto query)
        {
            var result = await _itemAppService.GetMine(query);
            return ToResponse(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult> Get(int id)
        {
            var result = await _itemAppService.GetMineById(id);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.ItemCreate)]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateItemInputDto input)
        {
            var result = await _itemAppService.Create(input);
            return ToResponse(result);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult> Update(int id, [FromBody] UpdateItemInputDto input)
        {
            var result = await _itemAppService.Update(id, input);
            return ToResponse(result);
        }

        [HttpPost("{id:int}/activate")]
        public async Task<ActionResult> Activate(int id)
        {
            var result = await _itemAppService.Activate(id);
            return ToResponse(result);
        }

        [HttpPost("{id:int}/archive")]
        public async Task<ActionResult> Archive(int id)
        {
            var result = await _itemAppService.ArchiveMine(id);
            return ToResponse(result);
        }
    }
}
