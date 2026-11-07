using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallCoupons;
using QPS.Mall.Application.Features.MallMemberCoupons;
using QPS.Mall.Contracts.DTOs.MallCoupons;
using QPS.Shared.Common;

namespace QPS.Mall.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/mall/coupons")]
public class MallCouponController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallCouponController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PaginationResponse<MallCouponDto>>> Query(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? couponType = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? keyword = null)
    {
        var (items, total) = await _mediator.Send(new QueryMallCouponsCommand
        {
            Request = new MallCouponQueryRequest
            {
                PageIndex = page,
                PageSize = pageSize,
                CouponType = couponType,
                IsActive = isActive,
                Keyword = keyword
            }
        });
        return Ok(new PaginationResponse<MallCouponDto>(items, total, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MallCouponDto>> Get(Guid id)
    {
        return Ok(await _mediator.Send(new GetMallCouponQuery { Id = id }));
    }

    [HttpPost]
    public async Task<ActionResult<MallCouponDto>> Create([FromBody] MallCouponCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallCouponCommand { Request = request }));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<MallCouponDto>> Update(Guid id, [FromBody] MallCouponUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallCouponCommand { Id = id, Request = request }));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<bool>> Delete(Guid id)
    {
        return Ok(await _mediator.Send(new DeleteMallCouponCommand { Id = id }));
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<bool>> ChangeStatus(Guid id, [FromQuery] bool isActive)
    {
        return Ok(await _mediator.Send(new ChangeMallCouponStatusCommand { Id = id, IsActive = isActive }));
    }

    [HttpPut("{id}/replenish")]
    public async Task<ActionResult<bool>> Replenish(Guid id, [FromQuery] int count)
    {
        return Ok(await _mediator.Send(new ReplenishMallCouponCommand { Id = id, Count = count }));
    }
}

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/mall/member-coupons")]
public class MallMemberCouponController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallMemberCouponController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<MallMemberCouponDto>>> GetByMember([FromQuery] MallMemberCouponQueryRequest request)
    {
        return Ok(await _mediator.Send(new GetMallMemberCouponsQuery { Request = request }));
    }

    [HttpPost("claim")]
    public async Task<ActionResult<MallMemberCouponDto>> Claim([FromQuery] Guid couponId, [FromQuery] Guid memberId)
    {
        return Ok(await _mediator.Send(new ClaimMallCouponCommand { CouponId = couponId, MemberId = memberId }));
    }

    [HttpPut("{id}/use")]
    public async Task<ActionResult<bool>> Use(Guid id, [FromQuery] string orderNo)
    {
        return Ok(await _mediator.Send(new UseMallMemberCouponCommand { Id = id, OrderNo = orderNo }));
    }
}