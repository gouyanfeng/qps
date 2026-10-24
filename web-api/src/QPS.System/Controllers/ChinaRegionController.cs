using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.System.Contracts.DTOs.ChinaRegions;
using QPS.System.Application.Features.ChinaRegions;

namespace QPS.System.Controllers;

[ApiController]
[Route("api/admin/china-regions")]
[AllowAnonymous]
public class ChinaRegionController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChinaRegionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChinaRegionDto>>> GetChinaRegions([FromQuery] bool activeOnly = true)
    {
        var result = await _mediator.Send(new GetChinaRegionsQuery { ActiveOnly = activeOnly });
        return Ok(result);
    }
}


