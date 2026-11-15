using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoAzul.Api.RateLimiting;
using NoAzul.Application.CustomCategories.Dtos;
using NoAzul.Application.CustomCategories.Interfaces;

namespace NoAzul.Api.Controllers
{
    [Route("api/custom-categories")]
    public class CustomCategoriesController : ApiBaseController
    {
        private readonly ICustomCategoryAppService _appService;

        public CustomCategoriesController(ICustomCategoryAppService appService)
        {
            _appService = appService;
        }

        [HttpGet]
        public async Task<ActionResult> List([FromQuery] CustomCategoryListQueryDto query)
        {
            var result = await _appService.GetMine(query);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.CustomCategory)]
        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CustomCategoryInputDto input)
        {
            var result = await _appService.Create(input);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.CustomCategory)]
        [HttpPut("{id:int}")]
        public async Task<ActionResult> Rename(int id, [FromBody] CustomCategoryInputDto input)
        {
            var result = await _appService.Rename(id, input);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.CustomCategory)]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult> Delete(int id)
        {
            var result = await _appService.Delete(id);
            return ToResponse(result);
        }
    }
}
