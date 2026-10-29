using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallMemberCoupons;
using QPS.Mall.Application.Features.MallCoupons;
using QPS.Mall.Application.Features.MallMembers;
using QPS.Mall.Contracts.DTOs.MallCoupons;
using QPS.Mall.Contracts.DTOs.MallMembers;
using QPS.Shared.Contracts;
using QPS.Shared.Extensions;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
[Authorize(Policy = "MallMember")]
public class MallMemberController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public MallMemberController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet("member/me")]
    public async Task<ActionResult<MallMemberDto>> GetProfile()
    {
        var memberId = _currentUser.GetUserId();
        return Ok(await _mediator.Send(new GetMallMemberQuery { Id = memberId }));
    }

    [HttpPut("member/me")]
    public async Task<ActionResult<MallMemberDto>> UpdateProfile([FromBody] MallMemberUpdateRequest request)
    {
        var memberId = _currentUser.GetUserId();
        return Ok(await _mediator.Send(new UpdateMallMemberCommand { Id = memberId, Request = request }));
    }

    [HttpPut("member/me/password")]
    public async Task<ActionResult<bool>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var memberId = _currentUser.GetUserId();
        return Ok(await _mediator.Send(new ChangeMallMemberPasswordCommand
        {
            MemberId = memberId,
            OldPassword = request.OldPassword,
            NewPassword = request.NewPassword
        }));
    }

    [HttpGet("member/me/points")]
    public async Task<ActionResult<List<MallMemberPointLogDto>>> GetPointLogs([FromQuery] int limit = 50)
    {
        var memberId = _currentUser.GetUserId();
        return Ok(await _mediator.Send(new GetMallMemberPointLogsQuery { MemberId = memberId, Limit = limit }));
    }

    [HttpGet("member/me/order-badge")]
    public async Task<ActionResult<MallMemberOrderBadgeDto>> GetOrderBadge([FromQuery] Guid storeId)
    {
        var memberId = _currentUser.GetUserId();
        return Ok(await _mediator.Send(new GetMallMemberOrderBadgeQuery { MemberId = memberId, StoreId = storeId }));
    }

    [HttpGet("member/me/coupons")]
    public async Task<ActionResult<List<MallMemberCouponDto>>> GetMyCoupons([FromQuery] string? status = null)
    {
        var memberId = _currentUser.GetUserId();
        return Ok(await _mediator.Send(new GetMallMemberCouponsQuery
        {
            Request = new MallMemberCouponQueryRequest
            {
                MemberId = memberId,
                Status = status
            }
        }));
    }

    [HttpGet("member/me/coupon-count")]
    public async Task<ActionResult<int>> GetMyCouponCount()
    {
        var memberId = _currentUser.GetUserId();
        return Ok(await _mediator.Send(new GetMallMemberCouponCountQuery { MemberId = memberId }));
    }

    [HttpPost("member/me/coupons/{couponId}/claim")]
    public async Task<ActionResult<MallMemberCouponDto>> ClaimCoupon(Guid couponId)
    {
        var memberId = _currentUser.GetUserId();
        return Ok(await _mediator.Send(new ClaimMallCouponCommand { CouponId = couponId, MemberId = memberId }));
    }
}

public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}