using BlazorTemplate.DTO.Api;
using Microsoft.AspNetCore.Mvc;

namespace BlazorTemplate.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected ActionResult<ApiResponse<T>> Success<T>(T data, string message = "Success")
        {
            return Ok(new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            });
        }

        protected ActionResult<ApiResponse<object>> Success(string message = "Success")
        {
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = message
            });
        }

        protected ActionResult<PagedApiResponse<T>> Success<T>(IEnumerable<T> data, int page, int pageSize, int totalCount, string message = "Success")
        {
            return Ok(new PagedApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        protected ActionResult<ApiResponse<object>> Error(string message, int statusCode = 400, List<string>? errors = null)
        {
            return StatusCode(statusCode, new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            });
        }

        protected ActionResult<ApiResponse<T>> Error<T>(string message, int statusCode = 400, List<string>? errors = null)
        {
            return StatusCode(statusCode, new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>()
            });
        }

        protected ActionResult<ApiResponse<object>> BadRequest(string message, List<string>? errors = null)
        {
            return Error(message, 400, errors);
        }

        protected ActionResult<ApiResponse<T>> BadRequest<T>(string message, List<string>? errors = null)
        {
            return Error<T>(message, 400, errors);
        }

        protected ActionResult<ApiResponse<object>> NotFound(string message = "Resource not found")
        {
            return Error(message, 404);
        }

        protected ActionResult<ApiResponse<T>> NotFound<T>(string message = "Resource not found")
        {
            return Error<T>(message, 404);
        }

        protected ActionResult<ApiResponse<object>> Unauthorized(string message = "Unauthorized")
        {
            return Error(message, 401);
        }

        protected ActionResult<ApiResponse<T>> Unauthorized<T>(string message = "Unauthorized")
        {
            return Error<T>(message, 401);
        }

        protected ActionResult<ApiResponse<object>> Forbidden(string message = "Forbidden")
        {
            return Error(message, 403);
        }

        protected ActionResult<ApiResponse<T>> Forbidden<T>(string message = "Forbidden")
        {
            return Error<T>(message, 403);
        }

        protected ActionResult<ApiResponse<object>> Conflict(string message, List<string>? errors = null)
        {
            return Error(message, 409, errors);
        }

        protected ActionResult<ApiResponse<T>> Conflict<T>(string message, List<string>? errors = null)
        {
            return Error<T>(message, 409, errors);
        }

        protected ActionResult<ApiResponse<object>> UnprocessableEntity(string message, List<string>? errors = null)
        {
            return Error(message, 422, errors);
        }

        protected ActionResult<ApiResponse<T>> UnprocessableEntity<T>(string message, List<string>? errors = null)
        {
            return Error<T>(message, 422, errors);
        }

        protected List<string> GetModelStateErrors()
        {
            return ModelState.SelectMany(x => x.Value?.Errors ?? new Microsoft.AspNetCore.Mvc.ModelBinding.ModelErrorCollection())
                           .Select(x => x.ErrorMessage)
                           .ToList();
        }

        protected string GetCurrentUserId()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        protected string GetCurrentUserEmail()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        }

        protected bool IsInRole(string role)
        {
            return User.IsInRole(role);
        }
    }
}