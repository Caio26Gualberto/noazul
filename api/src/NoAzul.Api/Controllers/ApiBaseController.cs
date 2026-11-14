using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NoAzul.Api.Controllers
{
    [ApiController]
    [Authorize]
    public abstract class ApiBaseController : BaseController
    {
    }
}
