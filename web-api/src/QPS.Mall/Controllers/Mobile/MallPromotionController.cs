using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallPromotions;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Controllers.Mobile;

/// <summary>
/// 移动端促销接口。
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/mall/promotions")]
public class MallPromotionController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallPromotionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 查询当前订单金额可用的换购活动（适用源为商品、满额门槛的 TradeIn 促销）。
    /// </summary>
    [HttpGet("exchange")]
    public async Task<ActionResult<List<MallExchangeOfferDto>>> GetExchangeOffers(
        [FromQuery] decimal amount, [FromQuery] Guid? memberId)
    {
        var query = new GetMallExchangeOffersQuery { Amount = amount, MemberId = memberId };
        var offers = await _mediator.Send(query);
        return Ok(offers);
    }
}
