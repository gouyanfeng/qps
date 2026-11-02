using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Crm.Contracts.DTOs;
using QPS.Crm.Contracts.DTOs.FollowTasks;
using QPS.Crm.Application.Features.FollowTasks;

namespace QPS.Crm.Controllers;

[Route("api/admin/crm/follow-tasks")]
[ApiController]
public class CrmFollowTaskController : ControllerBase
{
    private readonly IMediator _mediator;
    public CrmFollowTaskController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<CrmFollowTaskResponse>> Get([FromQuery] GetCrmFollowTasksQuery query)
        => Ok(await _mediator.Send(query));

    [HttpGet("overview")]
    public async Task<ActionResult<CrmFollowTaskOverviewDto>> GetOverview()
        => Ok(await _mediator.Send(new GetCrmFollowTaskOverviewQuery()));
}
