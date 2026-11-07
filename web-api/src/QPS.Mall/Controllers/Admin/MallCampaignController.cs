using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallCampaigns;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Controllers;

/// <summary>
/// 管理端活动专题维护（含选品）。
/// </summary>
[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/mall")]
public class MallCampaignController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallCampaignController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 分页查询活动专题。
    /// </summary>
    [HttpGet("campaigns")]
    public async Task<ActionResult<PaginationResponse<MallCampaignListItemDto>>> GetCampaigns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int? type = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _mediator.Send(new GetMallCampaignsQuery
        {
            Page = page,
            PageSize = pageSize,
            Keyword = keyword,
            IsActive = isActive,
            Type = type
        }, cancellationToken));
    }

    /// <summary>
    /// 分页查询可选券（领券型活动配置用）。
    /// </summary>
    [HttpGet("campaigns/coupons")]
    public async Task<ActionResult<PaginationResponse<MallCampaignCouponOptionDto>>> GetCouponOptions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _mediator.Send(new GetMallCampaignCouponOptionsQuery
        {
            Page = page,
            PageSize = pageSize,
            Keyword = keyword
        }, cancellationToken));
    }

    /// <summary>
    /// 查询活动专题详情（含已选商品）。
    /// </summary>
    [HttpGet("campaigns/{id:guid}")]
    public async Task<ActionResult<MallCampaignDto>> GetCampaign(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetMallCampaignQuery { Id = id }, cancellationToken));
    }

    /// <summary>
    /// 新增活动专题（含选品）。
    /// </summary>
    [HttpPost("campaigns")]
    public async Task<ActionResult<Guid>> CreateCampaign(
        [FromBody] MallCampaignCreateRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new CreateMallCampaignCommand { Request = request }, cancellationToken));
    }

    /// <summary>
    /// 编辑活动专题（选品整体替换）。
    /// </summary>
    [HttpPut("campaigns/{id:guid}")]
    public async Task<ActionResult<Guid>> UpdateCampaign(
        Guid id,
        [FromBody] MallCampaignUpdateRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new UpdateMallCampaignCommand
        {
            Id = id,
            Request = request
        }, cancellationToken));
    }

    /// <summary>
    /// 活动专题上架 / 下架。
    /// </summary>
    [HttpPatch("campaigns/{id:guid}/status")]
    public async Task<ActionResult<bool>> ChangeCampaignStatus(
        Guid id,
        [FromBody] MallCampaignStatusRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new ChangeMallCampaignStatusCommand
        {
            Id = id,
            IsActive = request.IsActive
        }, cancellationToken));
    }

    /// <summary>
    /// 删除活动专题（软删除，同时删除选品）。
    /// </summary>
    [HttpDelete("campaigns/{id:guid}")]
    public async Task<ActionResult<bool>> DeleteCampaign(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new DeleteMallCampaignCommand { Id = id }, cancellationToken));
    }
}
