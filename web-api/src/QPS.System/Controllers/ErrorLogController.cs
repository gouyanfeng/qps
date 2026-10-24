using QPS.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.System.Contracts.DTOs.ErrorLogs;
using QPS.Shared.Extensions;
using QPS.System.Application.Features.ErrorLogs;

namespace QPS.System.Controllers;

[ApiController]
[Route("api/admin/error-logs")]
[Authorize]
public class ErrorLogController : ControllerBase
{
    private readonly IMediator _mediator;

    public ErrorLogController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginationResponse<ErrorLogDto>>> GetErrorLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? errorType = null,
        [FromQuery] string? errorMessage = null,
        [FromQuery] string? requestUrl = null,
        [FromQuery] string? username = null,
        [FromQuery] int? httpStatusCode = null,
        [FromQuery] DateTime? startAt = null,
        [FromQuery] DateTime? endAt = null)
    {
        var result = await _mediator.Send(new GetErrorLogsQuery
        {
            Page = page,
            PageSize = pageSize,
            ErrorType = errorType,
            ErrorMessage = errorMessage,
            RequestUrl = requestUrl,
            Username = username,
            HttpStatusCode = httpStatusCode,
            StartAt = startAt,
            EndAt = endAt
        });

        return Ok(result);
    }
}
