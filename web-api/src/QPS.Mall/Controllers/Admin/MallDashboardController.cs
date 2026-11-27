using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Mall.Application.Features.MallDashboard;

namespace QPS.Mall.Controllers;

/// <summary>
/// 商城数据看板：核心经营指标（固定近 7 天）。
/// </summary>
[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/mall/dashboard")]
public class MallDashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public MallDashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("order-trend")]
    public async Task<ActionResult<object>> GetOrderTrend()
    {
        return Ok(await _mediator.Send(new GetMallDashboardChartQuery(MallDashboardChart.OrderTrend)));
    }

    [HttpGet("sales-trend")]
    public async Task<ActionResult<object>> GetSalesTrend()
    {
        return Ok(await _mediator.Send(new GetMallDashboardChartQuery(MallDashboardChart.SalesTrend)));
    }

    [HttpGet("order-status-distribution")]
    public async Task<ActionResult<object>> GetOrderStatusDistribution()
    {
        return Ok(await _mediator.Send(new GetMallDashboardChartQuery(MallDashboardChart.OrderStatusDistribution)));
    }

    [HttpGet("top-products")]
    public async Task<ActionResult<object>> GetTopProducts()
    {
        return Ok(await _mediator.Send(new GetMallDashboardChartQuery(MallDashboardChart.TopProducts)));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetSummary()
    {
        return Ok(await _mediator.Send(new GetMallDashboardChartQuery(MallDashboardChart.Summary)));
    }

    [HttpGet("aov-trend")]
    public async Task<ActionResult<object>> GetAovTrend()
    {
        return Ok(await _mediator.Send(new GetMallDashboardChartQuery(MallDashboardChart.AovTrend)));
    }

    [HttpGet("conversion-funnel")]
    public async Task<ActionResult<object>> GetConversionFunnel()
    {
        return Ok(await _mediator.Send(new GetMallDashboardChartQuery(MallDashboardChart.ConversionFunnel)));
    }
}
