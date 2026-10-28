using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallNotices;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
public class MallNoticeController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallNoticeController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("notices")]
    public async Task<ActionResult<List<MallNoticeDto>>> GetActive([FromQuery] Guid storeId)
    {
        return Ok(await _mediator.Send(new GetActiveMallNoticesQuery { StoreId = storeId }));
    }
}