using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallActivities;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers;

[ApiController]
[Route("api/admin/mall")]
public class MallActivityController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallActivityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("activities")]
    public async Task<ActionResult<List<MallActivityListItemDto>>> GetActivities(
        [FromQuery] string? keyword = null,
        [FromQuery] bool? isActive = null)
    {
        return Ok(await _mediator.Send(new GetMallActivitiesQuery
        {
            Keyword = keyword,
            IsActive = isActive
        }));
    }

    [HttpGet("activities/{activityId:guid}")]
    public async Task<ActionResult<MallActivityDto>> GetActivity(Guid activityId)
    {
        return Ok(await _mediator.Send(new GetMallActivityQuery { Id = activityId }));
    }

    [HttpPost("activities")]
    public async Task<ActionResult<Guid>> CreateActivity([FromBody] MallActivityCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallActivityCommand { Request = request }));
    }

    [HttpPut("activities/{activityId:guid}")]
    public async Task<ActionResult<Guid>> UpdateActivity(
        Guid activityId,
        [FromBody] MallActivityUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallActivityCommand
        {
            Id = activityId,
            Request = request
        }));
    }

    [HttpPatch("activities/{activityId:guid}/status")]
    public async Task<ActionResult<bool>> ChangeActivityStatus(
        Guid activityId,
        [FromBody] MallActivityStatusRequest request)
    {
        return Ok(await _mediator.Send(new ChangeMallActivityStatusCommand
        {
            ActivityId = activityId,
            IsActive = request.IsActive
        }));
    }

    [HttpDelete("activities/{activityId:guid}")]
    public async Task<ActionResult<bool>> DeleteActivity(Guid activityId)
    {
        return Ok(await _mediator.Send(new DeleteMallActivityCommand { ActivityId = activityId }));
    }
}
