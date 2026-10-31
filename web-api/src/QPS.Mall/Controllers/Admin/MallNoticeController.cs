using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallNotices;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers;

[ApiController]
public class MallNoticeController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallNoticeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("api/admin/mall/notices")]
    public async Task<ActionResult<List<MallNoticeDto>>> GetList(bool? isActive)
    {
        return Ok(await _mediator.Send(new GetMallNoticesQuery { IsActive = isActive }));
    }

    [HttpGet("api/admin/mall/notices/{id:guid}")]
    public async Task<ActionResult<MallNoticeDto>> Get(Guid id)
    {
        return Ok(await _mediator.Send(new GetMallNoticeQuery { Id = id }));
    }

    [HttpPost("api/admin/mall/notices")]
    public async Task<ActionResult<MallNoticeDto>> Create([FromBody] MallNoticeCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallNoticeCommand { Request = request }));
    }

    [HttpPut("api/admin/mall/notices/{id:guid}")]
    public async Task<ActionResult<MallNoticeDto>> Update(Guid id, [FromBody] MallNoticeUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallNoticeCommand { Id = id, Request = request }));
    }

    [HttpDelete("api/admin/mall/notices/{id:guid}")]
    public async Task<ActionResult<bool>> Delete(Guid id)
    {
        return Ok(await _mediator.Send(new DeleteMallNoticeCommand { Id = id }));
    }

    [HttpPut("api/admin/mall/notices/{id:guid}/status")]
    public async Task<ActionResult<bool>> ChangeStatus(Guid id, [FromQuery] bool isActive)
    {
        return Ok(await _mediator.Send(new ChangeMallNoticeStatusCommand { Id = id, IsActive = isActive }));
    }

    [HttpPut("api/admin/mall/notices/{id:guid}/top")]
    public async Task<ActionResult<bool>> SetTop(Guid id, [FromQuery] bool isTop)
    {
        return Ok(await _mediator.Send(new SetMallNoticeTopCommand { Id = id, IsTop = isTop }));
    }

    [HttpGet("api/mall/notices")]
    public async Task<ActionResult<List<MallNoticeDto>>> GetActive()
    {
        return Ok(await _mediator.Send(new GetActiveMallNoticesQuery()));
    }
}