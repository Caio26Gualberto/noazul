using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoAzul.Application.BackgroundJobs.Dtos;
using NoAzul.Application.BackgroundJobs.Interfaces;
using NoAzul.Domain.Constants;
using NoAzul.Domain.Enums.Jobs;

namespace NoAzul.Api.Controllers.Admin
{
    [Route("api/admin/jobs")]
    public class JobsController : ApiBaseController
    {
        private readonly IBackgroundJobAppService _backgroundJobAppService;

        public JobsController(IBackgroundJobAppService backgroundJobAppService)
        {
            _backgroundJobAppService = backgroundJobAppService;
        }

        [HttpGet("recurring")]
        [Authorize(Policy = Permissions.System.JobsView)]
        public async Task<ActionResult> Recurring()
        {
            var result = await _backgroundJobAppService.GetRecurring();
            return ToResponse(result);
        }

        [HttpGet("runs")]
        [Authorize(Policy = Permissions.System.JobsView)]
        public async Task<ActionResult> Runs([FromQuery] BackgroundJobQueryDto query)
        {
            var result = await _backgroundJobAppService.GetRuns(query);
            return ToResponse(result);
        }

        [HttpGet("runs/{id:int}")]
        [Authorize(Policy = Permissions.System.JobsView)]
        public async Task<ActionResult> Run(int id)
        {
            var result = await _backgroundJobAppService.GetRun(id);
            return ToResponse(result);
        }

        [HttpPost("items-snapshot")]
        [Authorize(Policy = Permissions.System.JobsManage)]
        public async Task<ActionResult> ItemsSnapshot()
        {
            var result = await _backgroundJobAppService.Trigger(EBackgroundJobType.ItemsSnapshot);
            return ToResponse(result);
        }

        [HttpPost("runs/{id:int}/retry")]
        [Authorize(Policy = Permissions.System.JobsManage)]
        public async Task<ActionResult> Retry(int id)
        {
            var result = await _backgroundJobAppService.Retry(id);
            return ToResponse(result);
        }
    }
}
