using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Contracts.Crm;
using QPS.Application.Contracts.Crm.FollowTasks;
using QPS.Application.Features.Crm.FollowTasks;

namespace QPS.WebAPI.Controllers.Admin.Crm;

[Route("api/admin/crm/follow-tasks")]
[ApiController]
public class CrmFollowTaskController : ControllerBase
{
    private readonly IMediator _mediator;
    public CrmFollowTaskController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<CrmFollowTaskResponse>> Get([FromQuery] GetCrmFollowTasksQuery query)
        => Ok(await _mediator.Send(query));
}
