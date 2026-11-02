using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallPages;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers;

[ApiController]
public class MallPageController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallPageController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("api/admin/mall/pages")]
    public async Task<ActionResult<List<MallPageDto>>> GetList()
    {
        return Ok(await _mediator.Send(new GetMallPagesQuery()));
    }

    [HttpGet("api/admin/mall/pages/{id:guid}")]
    public async Task<ActionResult<MallPageDto>> Get(Guid id)
    {
        return Ok(await _mediator.Send(new GetMallPageQuery { Id = id }));
    }

    [HttpPost("api/admin/mall/pages")]
    public async Task<ActionResult<MallPageDto>> Create([FromBody] MallPageCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallPageCommand { Request = request }));
    }

    [HttpPut("api/admin/mall/pages/{id:guid}")]
    public async Task<ActionResult<MallPageDto>> Update(Guid id, [FromBody] MallPageUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallPageCommand { Id = id, Request = request }));
    }

    [HttpDelete("api/admin/mall/pages/{id:guid}")]
    public async Task<ActionResult<bool>> Delete(Guid id)
    {
        return Ok(await _mediator.Send(new DeleteMallPageCommand { Id = id }));
    }

    [HttpPut("api/admin/mall/pages/{id:guid}/status")]
    public async Task<ActionResult<bool>> ChangeStatus(Guid id, [FromQuery] bool isActive)
    {
        return Ok(await _mediator.Send(new ChangeMallPageStatusCommand { Id = id, IsActive = isActive }));
    }
}