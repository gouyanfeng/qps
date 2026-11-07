using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallProducts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Controllers.Admin;

/// <summary>
/// 管理端商品评价审核、回复与删除。
/// </summary>
[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/mall/reviews")]
public class MallProductReviewController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallProductReviewController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 分页查询商品评价。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginationResponse<MallProductReviewAdminDto>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] int? rating = null,
        [FromQuery] int? auditStatus = null,
        [FromQuery] string? tag = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMallProductReviewsAdminQuery
        {
            Page = page,
            PageSize = pageSize,
            Keyword = keyword,
            ProductId = productId,
            Rating = rating,
            AuditStatus = auditStatus,
            Tag = tag
        };

        return Ok(await _mediator.Send(query, cancellationToken));
    }

    /// <summary>
    /// 变更评价审核状态。
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<bool>> ChangeStatus(
        Guid id,
        [FromBody] MallProductReviewAuditStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _mediator.Send(
            new ChangeMallProductReviewStatusCommand { Id = id, Request = request },
            cancellationToken));
    }

    /// <summary>
    /// 设置评价是否在商品详情展示及排序值。
    /// </summary>
    [HttpPatch("{id:guid}/featured")]
    public async Task<ActionResult<MallProductReviewAdminDto>> ChangeFeatured(
        Guid id,
        [FromBody] MallProductReviewFeaturedRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _mediator.Send(
            new ChangeMallProductReviewFeaturedCommand { Id = id, Request = request },
            cancellationToken));
    }

    /// <summary>
    /// 回复评价。
    /// </summary>
    [HttpPut("{id:guid}/reply")]
    public async Task<ActionResult<bool>> Reply(
        Guid id,
        [FromBody] MallProductReviewReplyRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _mediator.Send(
            new ReplyMallProductReviewCommand { Id = id, Request = request },
            cancellationToken));
    }

    /// <summary>
    /// 删除评价（软删除）。
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<bool>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        return Ok(await _mediator.Send(new DeleteMallProductReviewCommand { Id = id }, cancellationToken));
    }
}
