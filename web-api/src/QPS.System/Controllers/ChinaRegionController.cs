using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.System.Contracts.DTOs.ChinaRegions;
using QPS.System.Application.Features.ChinaRegions;
using QPS.Shared.Common;

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
    public async Task<ActionResult<PaginationResponse<ChinaRegionDto>>> GetChinaRegions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? code = null,
        [FromQuery] string? name = null,
        [FromQuery] int? level = null,
        [FromQuery] bool? isActive = null)
    {
        var result = await _mediator.Send(new GetChinaRegionsQuery
        {
            Page = page,
            PageSize = pageSize,
            Code = code,
            Name = name,
            Level = level,
            IsActive = isActive
        });
        return Ok(result);
    }
}