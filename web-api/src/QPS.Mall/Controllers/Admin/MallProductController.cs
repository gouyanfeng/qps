using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallProducts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Controllers;

/// <summary>
/// 管理端商品与规格维护。
/// </summary>
[ApiController]
[Route("api/admin/mall")]
public class MallProductController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallProductController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 分页查询商品。
    /// </summary>
    [HttpGet("products")]
    public async Task<ActionResult<PaginationResponse<MallProductDto>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortField = "CreatedAt",
        [FromQuery] string sortDirection = "Descending",
        [FromQuery] string? keyword = null,
        [FromQuery] Guid? storeId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? tagCode = null)
    {
        var query = new GetMallProductsQuery
        {
            Page = page,
            PageSize = pageSize,
            SortField = sortField,
            SortDirection = sortDirection,
            Keyword = keyword,
            StoreId = storeId,
            IsActive = isActive,
            CategoryId = categoryId,
            TagCode = tagCode
        };

        return Ok(await _mediator.Send(query));
    }

    /// <summary>
    /// 查询商品详情（含规格）。
    /// </summary>
    [HttpGet("products/{id:guid}")]
    public async Task<ActionResult<MallProductDto>> GetProduct(Guid id)
    {
        return Ok(await _mediator.Send(new GetMallProductQuery { Id = id }));
    }

    /// <summary>
    /// 新增商品。
    /// </summary>
    [HttpPost("products")]
    public async Task<ActionResult<bool>> CreateProduct([FromBody] MallProductCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallProductCommand { Request = request }));
    }

    /// <summary>
    /// 编辑商品。
    /// </summary>
    [HttpPut("products/{id:guid}")]
    public async Task<ActionResult<bool>> UpdateProduct(Guid id, [FromBody] MallProductUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallProductCommand { Id = id, Request = request }));
    }

    /// <summary>
    /// 商品上下架。
    /// </summary>
    [HttpPatch("products/{id:guid}/status")]
    public async Task<ActionResult<bool>> ChangeProductStatus(Guid id, [FromBody] MallProductStatusRequest request)
    {
        return Ok(await _mediator.Send(new ChangeMallProductStatusCommand { Id = id, Request = request }));
    }

    /// <summary>
    /// 设置商品是否在首页精选展示。
    /// </summary>
    [HttpPatch("products/{id:guid}/hot")]
    public async Task<ActionResult<bool>> ChangeProductHot(Guid id, [FromBody] MallProductHotRequest request)
    {
        return Ok(await _mediator.Send(new ChangeMallProductHotCommand { Id = id, Request = request }));
    }

    /// <summary>
    /// 删除商品（软删除，含规格）。
    /// </summary>
    [HttpDelete("products/{id:guid}")]
    public async Task<ActionResult<bool>> DeleteProduct(Guid id)
    {
        return Ok(await _mediator.Send(new DeleteMallProductCommand { Id = id }));
    }

    /// <summary>
    /// 新增商品规格。
    /// </summary>
    [HttpPost("products/{productId:guid}/skus")]
    public async Task<ActionResult<bool>> CreateSku(Guid productId, [FromBody] MallSkuCreateRequest request)
    {
        return Ok(await _mediator.Send(new CreateMallSkuCommand { ProductId = productId, Request = request }));
    }

    /// <summary>
    /// 编辑商品规格。
    /// </summary>
    [HttpPut("skus/{id:guid}")]
    public async Task<ActionResult<bool>> UpdateSku(Guid id, [FromBody] MallSkuUpdateRequest request)
    {
        return Ok(await _mediator.Send(new UpdateMallSkuCommand { Id = id, Request = request }));
    }

    /// <summary>
    /// 规格启用或停用。
    /// </summary>
    [HttpPatch("skus/{id:guid}/status")]
    public async Task<ActionResult<bool>> ChangeSkuStatus(Guid id, [FromBody] MallSkuStatusRequest request)
    {
        return Ok(await _mediator.Send(new ChangeMallSkuStatusCommand { Id = id, Request = request }));
    }

    /// <summary>
    /// 删除商品规格（软删除）。
    /// </summary>
    [HttpDelete("skus/{id:guid}")]
    public async Task<ActionResult<bool>> DeleteSku(Guid id)
    {
        return Ok(await _mediator.Send(new DeleteMallSkuCommand { Id = id }));
    }
}
