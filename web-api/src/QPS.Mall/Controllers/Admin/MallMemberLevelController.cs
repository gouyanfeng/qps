using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallMemberLevels;
using QPS.Mall.Contracts.DTOs.MallMembers;

namespace QPS.Mall.Controllers;

[ApiController]
[Route("api/admin/mall/member-levels")]
public class MallMemberLevelController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallMemberLevelController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<MallMemberLevelDto>>> GetAll([FromQuery] MallMemberLevelQueryRequest request)
    {
        return Ok(await _mediator.Send(new GetMallMemberLevelsQuery { Request = request }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MallMemberLevelDto>> Get(Guid id)
    {
        return Ok(await _mediator.Send(new GetMallMemberLevelQuery { Id = id }));
    }

    [HttpPost]
    public async Task<ActionResult<MallMemberLevelDto>> Create([FromBody] MallMemberLevelCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallMemberLevelCommand { Request = request }));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MallMemberLevelDto>> Update(Guid id, [FromBody] MallMemberLevelUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallMemberLevelCommand { Id = id, Request = request }));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(Guid id)
    {
        return Ok(await _mediator.Send(new DeleteMallMemberLevelCommand { Id = id }));
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<bool>> ChangeStatus(Guid id, [FromQuery] bool isActive)
    {
        return Ok(await _mediator.Send(new ChangeMallMemberLevelStatusCommand { Id = id, IsActive = isActive }));
    }

    [HttpPut("{id}/set-default")]
    public async Task<ActionResult<bool>> SetDefault(Guid id)
    {
        return Ok(await _mediator.Send(new SetDefaultMallMemberLevelCommand { Id = id }));
    }
}