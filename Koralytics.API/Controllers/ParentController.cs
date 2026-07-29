using System.Threading.Tasks;
using Koralytics.API.Controllers.BaseController;
using Koralytics.Application.DTOs.Parent;
using Koralytics.Application.Services.Parent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Koralytics.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Parent,SystemAdmin")]
    public class ParentController : ApiBaseController
    {
        private readonly IParentService _parentService;

        public ParentController(IParentService parentService)
        {
            _parentService = parentService;
        }

        /// <summary>
        /// Retrieves the list of children (players) linked to the logged-in parent.
        /// </summary>
        [HttpGet("my-children")]
        public async Task<IActionResult> GetMyChildren()
        {
            var parentUserId = GetCurrentUserId();
            var children = await _parentService.GetMyChildrenAsync(parentUserId);
            return OkResponse(children, "Children retrieved successfully.");
        }
    }
}
