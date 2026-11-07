using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallProducts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Controllers.Mobile;

[ApiController]
[Route("api/mall")]
[AllowAnonymous]
public class MallProductController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallProductController(IMediator mediator) => _mediator = mediator;

    [HttpGet("products")]
    public async Task<ActionResult<PaginationResponse<MallProductDto>>> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] Guid? storeId = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool? isHot = null,
        [FromQuery] string? tagCode = null,
        [FromQuery] string? sort = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMallProductsQuery
        {
            Page = page,
            PageSize = pageSize,
            Keyword = keyword,
            StoreId = storeId,
            CategoryId = categoryId,
            IsHot = isHot,
            TagCode = tagCode,
            SortBy = sort,
            IsActive = true
        };
        return Ok(await _mediator.Send(query, cancellationToken));
    }

    [HttpGet("products/{id:guid}")]
    public async Task<ActionResult<MallProductDto>> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetMallProductQuery { Id = id }, cancellationToken));
    }

    [HttpGet("products/{id:guid}/reviews")]
    public async Task<ActionResult<MallProductReviewPageDto>> GetReviews(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5,
        [FromQuery] string? tag = null,
        [FromQuery] bool featuredOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMallProductReviewsQuery
        {
            ProductId = id,
            Page = page,
            PageSize = pageSize,
            Tag = tag,
            FeaturedOnly = featuredOnly
        };
        return Ok(await _mediator.Send(query, cancellationToken));
    }
}