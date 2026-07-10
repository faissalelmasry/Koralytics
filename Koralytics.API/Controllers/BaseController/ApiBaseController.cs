using Koralytics.Application.Common;

using Microsoft.AspNetCore.Mvc;

namespace Koralytics.API.Controllers.BaseController
{
    [ApiController]
    public class ApiBaseController : ControllerBase
    {
        protected IActionResult OkResponse<T>(T data, string message = "Success")
        {
            return Ok(CreateResponse(data, message, StatusCodes.Status200OK));
        }

        protected IActionResult CreatedResponse<T>(T data,string actionName, object? routeValues,string message = "Created successfully")
        {
            return CreatedAtAction(actionName, routeValues, CreateResponse(data, message, StatusCodes.Status201Created));
        }

        protected IActionResult AcceptedResponse<T>(T data, string message = "Accepted")
        {
            return Accepted(CreateResponse(data, message, StatusCodes.Status202Accepted));
        }

        protected IActionResult NoContentResponse(string _message = "Completed successfully")
        {
            return Ok(CreateResponse<object?>(null, _message, StatusCodes.Status200OK));
        }

       
        protected IActionResult DeletedResponse(string message = "Deleted successfully")
        {
            return Ok(CreateResponse<object?>(null, message, StatusCodes.Status200OK));
        }

        private static ApiResponse<T> CreateResponse<T>(T data, string message, int statusCode)
        {
            return new ApiResponse<T>
            {
                IsSuccess = true,
                StatusCode = statusCode,
                Message = message,
                Data = data
            };
        }
    }
}
