using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallStocks;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Controllers;

/// <summary>
/// 管理端库存维护与库存流水。
/// </summary>
[ApiController]
[Route("api/admin/mall")]
public class MallStockController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallStockController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// 分页查询规格库存。
    /// </summary>
    [HttpGet("skus")]
    public async Task<ActionResult<PaginationResponse<MallSkuStockDto>>> GetSkus(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? keyword = null,
        [FromQuery] bool? lowStock = null,
        [FromQuery] Guid? productId = null)
    {
        var query = new GetMallSkusQuery
        {
            Page = page,
            PageSize = pageSize,
            Keyword = keyword,
            LowStock = lowStock,
            ProductId = productId
        };

        return Ok(await _mediator.Send(query));
    }

    /// <summary>
    /// 手动调整规格库存。
    /// </summary>
    [HttpPost("skus/{id:guid}/adjust-stock")]
    public async Task<ActionResult<bool>> AdjustStock(Guid id, [FromBody] MallSkuAdjustStockRequest request)
    {
        return Ok(await _mediator.Send(new AdjustMallSkuStockCommand { Id = id, Request = request }));
    }

    /// <summary>
    /// 批量调整规格价格与库存。
    /// </summary>
    [HttpPost("skus/batch-adjust")]
    public async Task<ActionResult<bool>> BatchAdjust([FromBody] MallSkuBatchAdjustRequest request)
    {
        return Ok(await _mediator.Send(new BatchAdjustMallSkusCommand { Request = request }));
    }

    /// <summary>
    /// 分页查询库存变更流水。
    /// </summary>
    [HttpGet("stock-logs")]
    public async Task<ActionResult<PaginationResponse<MallStockLogDto>>> GetStockLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? skuId = null,
        [FromQuery] string? changeType = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        var query = new GetMallStockLogsQuery
        {
            Page = page,
            PageSize = pageSize,
            SkuId = skuId,
            ChangeType = changeType,
            StartTime = startTime,
            EndTime = endTime
        };

        return Ok(await _mediator.Send(query));
    }
}
