using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallCoupons;
using QPS.Mall.Contracts.DTOs.MallCoupons;
using QPS.Shared.Contracts;
using QPS.Shared.Extensions;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
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
    public async Task<ActionResult<List<MallCouponDto>>> GetAvailable([FromQuery] Guid storeId)
    {
        Guid? memberId = null;
        if (_currentUser?.IsAuthenticated() == true)
        {
            memberId = _currentUser.GetUserId();
        }
        return Ok(await _mediator.Send(new GetAvailableMallCouponsQuery { StoreId = storeId, MemberId = memberId }));
    }
}