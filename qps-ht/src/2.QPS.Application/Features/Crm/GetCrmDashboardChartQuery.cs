using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Features.Crm.CrmVendors;
using QPS.Application.Interfaces;

namespace QPS.Application.Features.Crm;

public enum CrmDashboardChart
{
    FollowFunnel,
    MainProductDistribution,
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
    private static readonly string[] EffectiveFollowResults = [CrmCodes.FollowResult.Connected, CrmCodes.FollowResult.Interested, "已接通", "有意向"];
    private static readonly (string Code, string Name)[] VendorPriorities = [("High", "高优先级"), ("Medium", "中优先级"), ("Low", "低优先级")];
    private readonly IDbContext _dbContext;

    public GetCrmDashboardChartHandler(IDbContext dbContext) => _dbContext = dbContext;

    public async Task<object> Handle(GetCrmDashboardChartQuery request, CancellationToken cancellationToken) => request.Chart switch
    {
        CrmDashboardChart.FollowFunnel => await GetFollowFunnelAsync(cancellationToken),
        CrmDashboardChart.MainProductDistribution => await GetMainProductDistributionAsync(cancellationToken),
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
        var statuses = new[] { new { Code = CrmCodes.Status.Pending, Name = "待联系" }, new { Code = CrmCodes.Status.Following, Name = "跟进中" }, new { Code = CrmCodes.Status.Interested, Name = "有意向" }, new { Code = CrmCodes.Status.Deal, Name = "成交" }, new { Code = CrmCodes.Status.Lost, Name = "流失" } };
        var counts = await _dbContext.CrmHerbBaseSubjects.Where(subject => !subject.IsDeleted).GroupBy(subject => subject.Status).Select(group => new { Status = group.Key, Count = group.Count() }).ToListAsync(cancellationToken);
        return statuses.Select(status => new CrmDashboardChartItemDto { Code = status.Code, Name = status.Name, Value = counts.FirstOrDefault(item => item.Status == status.Code)?.Count ?? 0 }).ToList();
    }

    private async Task<List<CrmDashboardChartItemDto>> GetMainProductDistributionAsync(CancellationToken cancellationToken)
    {
        var counts = await (from attribute in _dbContext.CrmBusinessEntityAttributes
                            join herbBase in _dbContext.CrmHerbBases on attribute.EntityId equals herbBase.Id
                            join subject in _dbContext.CrmHerbBaseSubjects on herbBase.HerbBaseSubjectId equals subject.Id
                            where !attribute.IsDeleted &&
                                  !herbBase.IsDeleted &&
                                  !subject.IsDeleted &&
                                  attribute.EntityType == CrmCodes.HerbBaseEntityType &&
                                  attribute.AttributeCode == CrmCodes.MainProductAttributeCode
                            group subject by attribute.AttributeValue into productGroup
                            select new
                            {
                                Code = productGroup.Key,
                                Count = productGroup.Select(subject => subject.Id).Distinct().Count()
                            })
            .ToListAsync(cancellationToken);
        return counts.Where(item => !string.IsNullOrWhiteSpace(item.Code)).Select(item => new CrmDashboardChartItemDto { Code = item.Code, Name = FormatProduct(item.Code), Value = item.Count }).OrderByDescending(item => item.Value).ThenBy(item => item.Name).ToList();
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
        var counts = await _dbContext.CrmVendors.Where(vendor => !vendor.IsDeleted).GroupBy(vendor => vendor.PriorityLevel).Select(group => new { PriorityLevel = group.Key, Count = group.Count() }).ToListAsync(cancellationToken);
        return VendorPriorities.Select(priority => new CrmDashboardChartItemDto { Code = priority.Code, Name = priority.Name, Value = counts.FirstOrDefault(item => item.PriorityLevel == priority.Code)?.Count ?? 0 }).ToList();
    }

    private async Task<List<CrmDashboardNewPurchaseDemandTrendItemDto>> GetNewPurchaseDemandTrendAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var trendStart = today.AddDays(-6);
        var dates = await (from demand in _dbContext.CrmPurchaseDemands join vendor in _dbContext.CrmVendors on demand.VendorId equals vendor.Id where !demand.IsDeleted && !vendor.IsDeleted && demand.CreatedAt >= trendStart && demand.CreatedAt < today.AddDays(1) select demand.CreatedAt).ToListAsync(cancellationToken);
        return Enumerable.Range(0, 7).Select(offset => trendStart.AddDays(offset)).Select(date => new CrmDashboardNewPurchaseDemandTrendItemDto { Date = date, NewPurchaseDemandCount = dates.Count(createdAt => createdAt >= date && createdAt < date.AddDays(1)) }).ToList();
    }

    private async Task<List<CrmDashboardChartItemDto>> GetVendorPurchaseProductDistributionAsync(CancellationToken cancellationToken)
    {
        var coverage = await (from demand in _dbContext.CrmPurchaseDemands join item in _dbContext.CrmPurchaseDemandItems on demand.Id equals item.PurchaseDemandId join vendor in _dbContext.CrmVendors on demand.VendorId equals vendor.Id where !demand.IsDeleted && !item.IsDeleted && !vendor.IsDeleted group demand by item.ProductName into productGroup select new { ProductName = productGroup.Key, VendorCount = productGroup.Select(demand => demand.VendorId).Distinct().Count() }).ToListAsync(cancellationToken);
        return coverage.Where(item => !string.IsNullOrWhiteSpace(item.ProductName)).Select(item => new CrmDashboardChartItemDto { Code = item.ProductName, Name = FormatProduct(item.ProductName), Value = item.VendorCount }).OrderByDescending(item => item.Value).ThenBy(item => item.Name).ToList();
    }

    private static string FormatProduct(string code) => code switch
    {
        "HUANG_QI" => "黄芪", "DANG_GUI" => "当归", "DANG_SHEN" => "党参", "TIAN_MA" => "天麻", "OTHER" => "其他", _ => code
    };
}
