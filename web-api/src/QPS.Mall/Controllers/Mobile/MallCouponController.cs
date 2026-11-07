using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallCoupons;
using QPS.Mall.Application.Features.MallMemberCoupons;
using QPS.Mall.Contracts.DTOs.MallCoupons;
using QPS.Shared.Contracts;
using QPS.Shared.Extensions;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
[Authorize(Policy = "MallMember")]
public class MallCouponController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService? _currentUser;

    public MallCouponController(IMediator mediator, ICurrentUserService? currentUser = null)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("coupons/available")]
    public async Task<ActionResult<List<MallCouponDto>>> GetAvailable()
    {
        Guid? memberId = null;
        if (_currentUser?.IsAuthenticated() == true)
        {
            memberId = _currentUser.GetUserId();
        }
        return Ok(await _mediator.Send(new GetAvailableMallCouponsQuery { MemberId = memberId }));
    }

    /// <summary>确认页选券：按当前结算商品返回可用券与不可用原因。</summary>
    [HttpPost("coupons/for-order")]
    public async Task<ActionResult<List<MallCheckoutCouponDto>>> GetForOrder(
        [FromBody] MallCheckoutCouponQueryRequest request)
    {
        return Ok(await _mediator.Send(new GetMallCheckoutCouponsQuery
        {
            MemberId = _currentUser!.GetUserId(),
            Request = request
        }));
    }
}