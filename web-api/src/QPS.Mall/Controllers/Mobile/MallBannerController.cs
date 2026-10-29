using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallBanners;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
[Authorize(Policy = "MallMember")]
public class MallBannerController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallBannerController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("banners")]
    public async Task<ActionResult<List<MallBannerDto>>> GetActive(
        [FromQuery] Guid storeId,
        [FromQuery] string? position,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(
            new GetActiveMallBannersQuery { StoreId = storeId, Position = position },
            cancellationToken));
    }
}