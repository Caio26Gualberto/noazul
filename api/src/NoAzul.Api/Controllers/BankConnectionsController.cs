using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NoAzul.Api.RateLimiting;
using NoAzul.Application.BankConnections.Dtos;
using NoAzul.Application.BankConnections.Interfaces;

namespace NoAzul.Api.Controllers
{
    [Route("api/bank-connections")]
    public class BankConnectionsController : ApiBaseController
    {
        private readonly IBankConnectionAppService _appService;

        public BankConnectionsController(IBankConnectionAppService appService)
        {
            _appService = appService;
        }

        [HttpGet]
        public async Task<ActionResult> List([FromQuery] BankConnectionListQueryDto query)
        {
            var result = await _appService.GetMine(query);
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.BankConnect)]
        [HttpPost("connect-token")]
        public async Task<ActionResult> CreateConnectToken()
        {
            var result = await _appService.CreateConnectToken();
            return ToResponse(result);
        }

        [EnableRateLimiting(ApiRateLimitPolicies.BankConnect)]
        [HttpPost]
        public async Task<ActionResult> Register([FromBody] RegisterBankConnectionInputDto input)
        {
            var result = await _appService.Register(input);
            return ToResponse(result);
        }
    }
}
