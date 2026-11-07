using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallMembers;
using QPS.Mall.Contracts.DTOs.MallMembers;

namespace QPS.Mall.Controllers.Admin;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/mall/members")]
public class MallMemberController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallMemberController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<(List<MallMemberDto> Items, int Total)>> Query([FromQuery] MallMemberQueryRequest request)
    {
        return Ok(await _mediator.Send(new QueryMallMembersCommand { Request = request }));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MallMemberDto>> Get(Guid id)
    {
        return Ok(await _mediator.Send(new GetMallMemberQuery { Id = id }));
    }

    [HttpGet("by-phone/{phone}")]
    public async Task<ActionResult<MallMemberDto?>> GetByPhone(string phone)
    {
        return Ok(await _mediator.Send(new GetMallMemberByPhoneQuery { Phone = phone }));
    }

    [HttpPost]
    public async Task<ActionResult<MallMemberDto>> Create([FromBody] MallMemberCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallMemberCommand { Request = request }));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MallMemberDto>> Update(Guid id, [FromBody] MallMemberUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallMemberCommand { Id = id, Request = request }));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(Guid id)
    {
        return Ok(await _mediator.Send(new DeleteMallMemberCommand { Id = id }));
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<bool>> ChangeStatus(Guid id, [FromQuery] bool isActive)
    {
        return Ok(await _mediator.Send(new ChangeMallMemberStatusCommand { Id = id, IsActive = isActive }));
    }

    [HttpPut("{id}/adjust-points")]
    public async Task<ActionResult<bool>> AdjustPoints(Guid id, [FromBody] MallMemberAdjustPointsRequest request)
    {
        return Ok(await _mediator.Send(new AdjustMallMemberPointsCommand { Id = id, Request = request }));
    }
}