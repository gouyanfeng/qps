using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Features.Crm.CrmVendors;
using QPS.Application.Interfaces;

namespace QPS.Application.Features.Crm;

// 保留给现有单元测试使用；控制器不再公开聚合仪表盘端点。
public class GetCrmDashboardQuery : IRequest<CrmDashboardDto>;

public class GetCrmDashboardHandler : IRequestHandler<GetCrmDashboardQuery, CrmDashboardDto>
{
    private readonly IDbContext _dbContext;

    public GetCrmDashboardHandler(IDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
    }

    public async Task<CrmDashboardDto> Handle(GetCrmDashboardQuery request, CancellationToken cancellationToken)
    {
        var chartHandler = new GetCrmDashboardChartHandler(_dbContext);
        var followFunnel = (List<CrmDashboardChartItemDto>)await chartHandler.Handle(new GetCrmDashboardChartQuery(CrmDashboardChart.FollowFunnel), cancellationToken);
        var highIntentSubjectCount = await _dbContext.CrmHerbBaseSubjects.CountAsync(subject => !subject.IsDeleted && subject.Status == CrmCodes.Status.Interested, cancellationToken);

        return new CrmDashboardDto
        {
            Metrics = new CrmDashboardMetricsDto { HighIntentSubjectCount = highIntentSubjectCount },
            FollowFunnel = followFunnel
        };
    }
}
