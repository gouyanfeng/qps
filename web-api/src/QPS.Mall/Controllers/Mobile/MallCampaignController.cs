using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallCampaigns;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Controllers.Mobile;

/// <summary>
/// 移动端活动专题（匿名可访问）。
/// </summary>
[ApiController]
[Route("api/mall")]
[AllowAnonymous]
public class MallCampaignPublicController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallCampaignPublicController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 进行中的活动专题列表。
    /// </summary>
    [HttpGet("campaigns")]
    public async Task<ActionResult<List<MallCampaignPublicListItemDto>>> GetPublished(
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetPublishedMallCampaignsQuery(), cancellationToken));
    }

    /// <summary>
    /// 活动专题详情（含图文介绍与状态）。
    /// </summary>
    [HttpGet("campaigns/{id:guid}")]
    public async Task<ActionResult<MallCampaignPublicDetailDto>> GetPublishedDetail(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetPublishedMallCampaignDetailQuery { Id = id }, cancellationToken));
    }

    /// <summary>
    /// 活动专题商品分页（商品 DTO 与促销富化与商品列表接口一致）。
    /// </summary>
    [HttpGet("campaigns/{id:guid}/products")]
    public async Task<ActionResult<PaginationResponse<MallCampaignProductDto>>> GetProducts(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _mediator.Send(new GetMallCampaignProductsQuery
        {
            CampaignId = id,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));
    }
}
