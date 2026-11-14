using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NoAzul.Application.AuditLogs.Dtos;
using NoAzul.Application.AuditLogs.Interfaces;
using NoAzul.Domain.Constants;

namespace NoAzul.Api.Controllers.Admin
{
    [Route("api/admin/audit")]
    public class AuditController : ApiBaseController
    {
        private readonly IAuditLogAppService _auditLogAppService;

        public AuditController(IAuditLogAppService auditLogAppService)
        {
            _auditLogAppService = auditLogAppService;
        }

        [HttpGet]
        [Authorize(Policy = Permissions.System.AuditView)]
        public async Task<ActionResult> List([FromQuery] AuditLogQueryDto query)
        {
            var result = await _auditLogAppService.GetAuditLogs(query);
            return ToResponse(result);
        }

        [HttpGet("filters")]
        [Authorize(Policy = Permissions.System.AuditView)]
        public async Task<ActionResult> Filters()
        {
            var result = await _auditLogAppService.GetFilters();
            return ToResponse(result);
        }
    }
}
