using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallPromotions;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers;

[ApiController]
[Route("api/admin/mall")]
public class MallPromotionController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallPromotionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("promotions")]
    public async Task<ActionResult<List<MallPromotionDto>>> GetPromotions(
        [FromQuery] string? keyword = null,
        [FromQuery] bool? isActive = null)
    {
        return Ok(await _mediator.Send(new GetMallPromotionsQuery
        {
            Keyword = keyword,
            IsActive = isActive
        }));
    }

    [HttpGet("promotions/{promotionId:guid}")]
    public async Task<ActionResult<MallPromotionDto>> GetPromotion(Guid promotionId)
    {
        return Ok(await _mediator.Send(new GetMallPromotionQuery { PromotionId = promotionId }));
    }

    [HttpPost("promotions")]
    public async Task<ActionResult<Guid>> CreatePromotion([FromBody] MallPromotionCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallPromotionCommand { Request = request }));
    }

    [HttpPut("promotions/{promotionId:guid}")]
    public async Task<ActionResult<bool>> UpdatePromotion(
        Guid promotionId,
        [FromBody] MallPromotionUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallPromotionCommand
        {
            PromotionId = promotionId,
            Request = request
        }));
    }

    [HttpPatch("promotions/{promotionId:guid}/status")]
    public async Task<ActionResult<bool>> ChangePromotionStatus(
        Guid promotionId,
        [FromQuery] bool isActive)
    {
        return Ok(await _mediator.Send(new ChangeMallPromotionStatusCommand
        {
            PromotionId = promotionId,
            IsActive = isActive
        }));
    }

    [HttpDelete("promotions/{promotionId:guid}")]
    public async Task<ActionResult<bool>> DeletePromotion(Guid promotionId)
    {
        return Ok(await _mediator.Send(new DeleteMallPromotionCommand { PromotionId = promotionId }));
    }
}