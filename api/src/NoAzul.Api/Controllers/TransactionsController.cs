using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoAzul.Api.RateLimiting;
using NoAzul.Application.Transactions.Dtos;
using NoAzul.Application.Transactions.Interfaces;

namespace NoAzul.Api.Controllers
{
    [Route("api/transactions")]
    public class TransactionsController : ApiBaseController
    {
        private readonly ITransactionAppService _appService;

        public TransactionsController(ITransactionAppService appService)
        {
            _appService = appService;
        }

        [HttpGet]
        public async Task<ActionResult> List([FromQuery] TransactionListQueryDto query)
        {
            var result = await _appService.GetMine(query);
            return ToResponse(result);
        }

        [HttpGet("monthly-summary")]
        public async Task<ActionResult> MonthlySummary([FromQuery] int months = 6)
        {
            var result = await _appService.GetMonthlySummary(months);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.TransactionCategory)]
        [HttpPut("{id:int}/category")]
        public async Task<ActionResult> UpdateCategory(int id, [FromBody] UpdateTransactionCategoryInputDto input)
        {
            var result = await _appService.UpdateCategory(id, input);
            return ToResponse(result);
        }
    }
}
