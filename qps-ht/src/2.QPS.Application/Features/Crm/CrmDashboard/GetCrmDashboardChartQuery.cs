using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm.CrmDashboard;
using QPS.Application.Features.Crm;
using QPS.Application.Features.Crm.CrmVendors;
using QPS.Application.Interfaces;

namespace QPS.Application.Features.Crm.CrmDashboard;

public enum CrmDashboardChart
{
    FollowFunnel,
    SupplyProductDistribution,
    FollowTrend,
    NewBaseTrend,
    VendorPriorityDistribution,
    VendorFollowTrend,
    NewPurchaseDemandTrend,
    VendorPurchaseProducts
}

public sealed record GetCrmDashboardChartQuery(CrmDashboardChart Chart) : IRequest<object>;

public class GetCrmDashboardChartHandler : IRequestHandler<GetCrmDashboardChartQuery, object>
{
    private static readonly string[] EffectiveFollowResults = [CrmCodes.FollowResult.Connected, CrmCodes.FollowResult.Interested];
    private static readonly string[] SubjectStatusOrder = [
        CrmCodes.Status.Pending,
        CrmCodes.Status.Following,
        CrmCodes.Status.Interested,
        CrmCodes.Status.Deal,
        CrmCodes.Status.Lost
    ];
    private readonly IDbContext _dbContext;

    public GetCrmDashboardChartHandler(IDbContext dbContext) => _dbContext = dbContext;

    public async Task<object> Handle(GetCrmDashboardChartQuery request, CancellationToken cancellationToken) => request.Chart switch
    {
        CrmDashboardChart.FollowFunnel => await GetFollowFunnelAsync(cancellationToken),
        CrmDashboardChart.SupplyProductDistribution => await GetSupplyProductDistributionAsync(cancellationToken),
        CrmDashboardChart.FollowTrend => await GetFollowTrendAsync(false, cancellationToken),
        CrmDashboardChart.NewBaseTrend => await GetNewBaseTrendAsync(cancellationToken),
        CrmDashboardChart.VendorPriorityDistribution => await GetVendorPriorityDistributionAsync(cancellationToken),
        CrmDashboardChart.VendorFollowTrend => await GetFollowTrendAsync(true, cancellationToken),
        CrmDashboardChart.NewPurchaseDemandTrend => await GetNewPurchaseDemandTrendAsync(cancellationToken),
        CrmDashboardChart.VendorPurchaseProducts => await GetVendorPurchaseProductDistributionAsync(cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(request.Chart), request.Chart, null)
    };

    private async Task<List<CrmDashboardChartItemDto>> GetFollowFunnelAsync(CancellationToken cancellationToken)
    {
        var counts = await _dbContext.CrmHerbBaseSubjects
            .Where(subject => !subject.IsDeleted && !string.IsNullOrWhiteSpace(subject.Status))
            .GroupBy(subject => subject.Status)
            .Select(group => new CrmDashboardChartItemDto
            {
                Code = group.Key,
                Name = group.Key,
                Value = group.Count()
            })
            .ToListAsync(cancellationToken);

        return counts
            .OrderBy(item =>
            {
                var statusIndex = Array.IndexOf(SubjectStatusOrder, item.Code);
                return statusIndex < 0 ? int.MaxValue : statusIndex;
            })
            .ThenBy(item => item.Name)
            .ToList();
    }

    private async Task<List<CrmDashboardChartItemDto>> GetSupplyProductDistributionAsync(CancellationToken cancellationToken)
    {
        var counts = await (from supply in _dbContext.CrmHerbBaseSupplies
                            join herbBase in _dbContext.CrmHerbBases on supply.HerbBaseId equals herbBase.Id
                            join subject in _dbContext.CrmHerbBaseSubjects on herbBase.HerbBaseSubjectId equals subject.Id
                            where !supply.IsDeleted &&
                                  !herbBase.IsDeleted &&
                                  !subject.IsDeleted
                            group subject by supply.ProductName into productGroup
                            select new
                            {
                                Code = productGroup.Key,
                                Count = productGroup.Select(subject => subject.Id).Distinct().Count()
                            })
            .ToListAsync(cancellationToken);
        return counts.Where(item => !string.IsNullOrWhiteSpace(item.Code)).Select(item => new CrmDashboardChartItemDto { Code = item.Code, Name = item.Code, Value = item.Count }).OrderByDescending(item => item.Value).ThenBy(item => item.Name).ToList();
    }

    private async Task<List<CrmDashboardTrendItemDto>> GetFollowTrendAsync(bool vendorChart, CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var trendStart = today.AddDays(-6);
        var entityType = vendorChart ? CrmCodes.VendorEntityType : CrmCodes.HerbBaseSubjectEntityType;
        var records = await _dbContext.CrmFollowRecords.Where(record => !record.IsDeleted && record.EntityType == entityType && record.CreatedAt >= trendStart && record.CreatedAt < today.AddDays(1)).Select(record => new { record.CreatedAt, record.FollowResult }).ToListAsync(cancellationToken);
        return Enumerable.Range(0, 7).Select(offset => trendStart.AddDays(offset)).Select(date =>
        {
            var nextDate = date.AddDays(1);
            var dailyRecords = records.Where(record => record.CreatedAt >= date && record.CreatedAt < nextDate).ToList();
            return new CrmDashboardTrendItemDto { Date = date, FollowCount = dailyRecords.Count, EffectiveFollowCount = dailyRecords.Count(record => EffectiveFollowResults.Contains(record.FollowResult)) };
        }).ToList();
    }

    private async Task<List<CrmDashboardNewBaseTrendItemDto>> GetNewBaseTrendAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var trendStart = today.AddDays(-6);
        var dates = await _dbContext.CrmHerbBases.Where(herbBase => herbBase.HerbBaseSubjectId.HasValue && !herbBase.IsDeleted && _dbContext.CrmHerbBaseSubjects.Any(subject => subject.Id == herbBase.HerbBaseSubjectId.Value && !subject.IsDeleted) && herbBase.CreatedAt >= trendStart && herbBase.CreatedAt < today.AddDays(1)).Select(herbBase => herbBase.CreatedAt).ToListAsync(cancellationToken);
        return Enumerable.Range(0, 7).Select(offset => trendStart.AddDays(offset)).Select(date => new CrmDashboardNewBaseTrendItemDto { Date = date, NewBaseCount = dates.Count(createdAt => createdAt >= date && createdAt < date.AddDays(1)) }).ToList();
    }

    private async Task<List<CrmDashboardChartItemDto>> GetVendorPriorityDistributionAsync(CancellationToken cancellationToken)
    {
        var counts = await _dbContext.CrmVendors
            .Where(vendor => !vendor.IsDeleted && !string.IsNullOrWhiteSpace(vendor.PriorityLevel))
            .GroupBy(vendor => vendor.PriorityLevel)
            .Select(group => new CrmDashboardChartItemDto
            {
                Code = group.Key,
                Name = group.Key,
                Value = group.Count()
            })
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

        return counts;
    }

    private async Task<List<CrmDashboardNewPurchaseDemandTrendItemDto>> GetNewPurchaseDemandTrendAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var trendStart = today.AddDays(-6);
        var dates = await (from demand in _dbContext.CrmVendorDemands join vendor in _dbContext.CrmVendors on demand.VendorId equals vendor.Id where !demand.IsDeleted && !vendor.IsDeleted && demand.CreatedAt >= trendStart && demand.CreatedAt < today.AddDays(1) select demand.CreatedAt).ToListAsync(cancellationToken);
        return Enumerable.Range(0, 7).Select(offset => trendStart.AddDays(offset)).Select(date => new CrmDashboardNewPurchaseDemandTrendItemDto { Date = date, NewPurchaseDemandCount = dates.Count(createdAt => createdAt >= date && createdAt < date.AddDays(1)) }).ToList();
    }

    private async Task<List<CrmDashboardChartItemDto>> GetVendorPurchaseProductDistributionAsync(CancellationToken cancellationToken)
    {
        var coverage = await (from demand in _dbContext.CrmVendorDemands join item in _dbContext.CrmVendorDemandItems on demand.Id equals item.VendorDemandId join vendor in _dbContext.CrmVendors on demand.VendorId equals vendor.Id where !demand.IsDeleted && !item.IsDeleted && !vendor.IsDeleted group demand by item.ProductName into productGroup select new { ProductName = productGroup.Key, VendorCount = productGroup.Select(demand => demand.VendorId).Distinct().Count() }).ToListAsync(cancellationToken);
        return coverage.Where(item => !string.IsNullOrWhiteSpace(item.ProductName)).Select(item => new CrmDashboardChartItemDto { Code = item.ProductName, Name = item.ProductName, Value = item.VendorCount }).OrderByDescending(item => item.Value).ThenBy(item => item.Name).ToList();
    }
}
