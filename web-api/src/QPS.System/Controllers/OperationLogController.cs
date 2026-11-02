using QPS.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.System.Contracts.DTOs.OperationLogs;
using QPS.Shared.Extensions;
using QPS.System.Application.Features.OperationLogs;

namespace QPS.System.Controllers;

[ApiController]
[Route("api/admin/operation-logs")]
[Authorize]
public class OperationLogController : ControllerBase
{
    private readonly IMediator _mediator;

    public OperationLogController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<PaginationResponse<OperationLogDto>>> GetOperationLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortField = "CreatedAt",
        [FromQuery] string sortDirection = "Descending",
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? actionType = null,
        [FromQuery] string? operatorName = null,
        [FromQuery] string? requestPath = null,
        [FromQuery] DateTime? startAt = null,
        [FromQuery] DateTime? endAt = null)
    {
        var result = await _mediator.Send(new GetOperationLogsQuery
        {
            Page = page,
            PageSize = pageSize,
            SortField = sortField,
            SortDirection = sortDirection,
            EntityType = entityType,
            EntityId = entityId,
            ActionType = actionType,
            OperatorName = operatorName,
            RequestPath = requestPath,
            StartAt = startAt,
            EndAt = endAt
        });

        return Ok(result);
    }
}
