using MediatR;
using Microsoft.AspNetCore.Mvc;
using QPS.Application.Features.Crm;

namespace QPS.WebAPI.Controllers.Admin.Crm;

[Route("api/admin/dashboard/crm")]
[ApiController]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("follow-funnel")]
    public Task<object> GetFollowFunnel() => GetChart(CrmDashboardChart.FollowFunnel);

    [HttpGet("main-product-distribution")]
    public Task<object> GetMainProductDistribution() => GetChart(CrmDashboardChart.MainProductDistribution);

    [HttpGet("follow-trend")]
    public Task<object> GetFollowTrend() => GetChart(CrmDashboardChart.FollowTrend);

    [HttpGet("new-base-trend")]
    public Task<object> GetNewBaseTrend() => GetChart(CrmDashboardChart.NewBaseTrend);

    [HttpGet("vendor-priority-distribution")]
    public Task<object> GetVendorPriorityDistribution() => GetChart(CrmDashboardChart.VendorPriorityDistribution);

    [HttpGet("vendor-follow-trend")]
    public Task<object> GetVendorFollowTrend() => GetChart(CrmDashboardChart.VendorFollowTrend);

    [HttpGet("new-purchase-demand-trend")]
    public Task<object> GetNewPurchaseDemandTrend() => GetChart(CrmDashboardChart.NewPurchaseDemandTrend);

    [HttpGet("vendor-purchase-product-distribution")]
    public Task<object> GetVendorPurchaseProductDistribution() => GetChart(CrmDashboardChart.VendorPurchaseProducts);

    private Task<object> GetChart(CrmDashboardChart chart)
    {
        return _mediator.Send(new GetCrmDashboardChartQuery(chart));
    }
}
