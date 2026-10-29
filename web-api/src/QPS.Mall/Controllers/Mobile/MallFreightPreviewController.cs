using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallFreightPreviews;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers;

[ApiController]
[Route("api/mall")]
[Authorize(Policy = "MallMember")]
public class MallFreightPreviewController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallFreightPreviewController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("freight-preview")]
    public async Task<ActionResult<MallFreightPreviewDto>> PreviewFreight(
        [FromBody] MallFreightPreviewRequest request)
    {
        return Ok(await _mediator.Send(new PreviewMallFreightCommand { Request = request }));
    }
}