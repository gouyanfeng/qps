using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallActivities;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
[Authorize(Policy = "MallMember")]
public class MallActivityPublicController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallActivityPublicController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("activities")]
    public async Task<ActionResult<List<MallActivityListItemDto>>> GetPublished(
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetPublishedMallActivitiesQuery(), cancellationToken));
    }

    [HttpGet("activities/{id:guid}")]
    public async Task<ActionResult<MallActivityDto>> GetPublishedDetail(Guid id)
    {
        return Ok(await _mediator.Send(new GetPublishedMallActivityDetailQuery { Id = id }));
    }
}
