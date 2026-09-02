using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Features.Crm.CrmVendors;
using QPS.Application.Interfaces;

namespace QPS.Application.Features.Crm;

public class GetCrmDashboardQuery : IRequest<CrmDashboardDto>;

public class GetCrmDashboardHandler : IRequestHandler<GetCrmDashboardQuery, CrmDashboardDto>
{
    private static readonly string[] ClosedStatuses = [CrmCodes.Status.Deal, CrmCodes.Status.Lost, "已成交", "已流失"];
    private static readonly string[] EffectiveFollowResults = [CrmCodes.FollowResult.Connected, CrmCodes.FollowResult.Interested, "已接通", "有意向"];
    private static readonly (string Code, string Name)[] VendorPriorities = [("High", "高优先级"), ("Medium", "中优先级"), ("Low", "低优先级")];

    private readonly IDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetCrmDashboardHandler(IDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<CrmDashboardDto> Handle(GetCrmDashboardQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var todayStart = now.Date;
        var tomorrowStart = todayStart.AddDays(1);
        var trendStart = todayStart.AddDays(-6);
        var subjects = _dbContext.CrmHerbBaseSubjects
            .Where(subject => !subject.IsDeleted);
        var vendors = _dbContext.CrmVendors
            .Where(vendor => !vendor.IsDeleted);
        var activeSubjects = subjects.Where(subject => !ClosedStatuses.Contains(subject.Status));

        var todayFollowCount = await activeSubjects.CountAsync(
            subject => subject.NextFollowAt >= todayStart && subject.NextFollowAt < tomorrowStart,
            cancellationToken);
        var overdueFollowCount = await activeSubjects.CountAsync(
            subject => subject.NextFollowAt.HasValue && subject.NextFollowAt.Value < now,
            cancellationToken);
        var mySubjectCount = await subjects.CountAsync(cancellationToken);
        var highIntentSubjectCount = await activeSubjects.CountAsync(
            subject => subject.Status == CrmCodes.Status.Interested, cancellationToken);

        var todayFollowSubjects = await activeSubjects
            .Where(subject => subject.NextFollowAt.HasValue && subject.NextFollowAt.Value < tomorrowStart)
            .OrderBy(subject => subject.NextFollowAt >= now)
            .ThenByDescending(subject => subject.Status == CrmCodes.Status.Interested)
            .ThenBy(subject => subject.NextFollowAt)
            .Take(10)
            .Select(subject => new CrmDashboardFollowSubjectDto
            {
                Id = subject.Id,
                SubjectName = subject.SubjectName ?? string.Empty,
                Grade = subject.Grade ?? string.Empty,
                PrimaryContactName = subject.PrimaryContactName ?? string.Empty,
                PrimaryContactPhone = subject.PrimaryContactPhone ?? string.Empty,
                LastFollowResult = subject.LastFollowResult ?? string.Empty,
                NextFollowAt = subject.NextFollowAt
            })
            .ToListAsync(cancellationToken);

        var recentFollowRecords = await (
                from record in _dbContext.CrmFollowRecords
                join subject in subjects on record.EntityId equals subject.Id
                where !record.IsDeleted && record.EntityType == CrmCodes.HerbBaseSubjectEntityType
                orderby record.CreatedAt descending
                select new CrmDashboardRecentFollowRecordDto
                {
                    Id = record.Id,
                    HerbBaseSubjectId = subject.Id,
                    SubjectName = subject.SubjectName ?? string.Empty,
                    FollowType = record.FollowType,
                    FollowResult = record.FollowResult,
                    IntentLevel = record.IntentLevel,
                    Content = record.Content,
                    NextFollowAt = record.NextFollowAt,
                    CreatedAt = record.CreatedAt
                })
            .Take(5)
            .ToListAsync(cancellationToken);

        var funnelStatuses = new[]
        {
            new { Code = CrmCodes.Status.Pending, Name = "待联系" },
            new { Code = CrmCodes.Status.Following, Name = "跟进中" },
            new { Code = CrmCodes.Status.Interested, Name = "有意向" },
            new { Code = CrmCodes.Status.Deal, Name = "成交" },
            new { Code = CrmCodes.Status.Lost, Name = "流失" }
        };
        var statusCounts = await subjects
            .GroupBy(subject => subject.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var followFunnel = funnelStatuses
            .Select(status => new CrmDashboardChartItemDto
            {
                Code = status.Code,
                Name = status.Name,
                Value = statusCounts.FirstOrDefault(item => item.Status == status.Code)?.Count ?? 0
            })
            .ToList();

        var mainProductCounts = await _dbContext.CrmBusinessEntityAttributes
            .Where(attribute =>
                !attribute.IsDeleted &&
                attribute.EntityType == CrmCodes.HerbBaseEntityType &&
                attribute.AttributeCode == CrmCodes.MainProductAttributeCode &&
                _dbContext.CrmHerbBases
                    .Where(herbBase => herbBase.HerbBaseSubjectId.HasValue && subjects.Select(subject => subject.Id).Contains(herbBase.HerbBaseSubjectId.Value))
                    .Select(herbBase => herbBase.Id)
                    .Contains(attribute.EntityId))
            .GroupBy(attribute => attribute.AttributeValue)
            .Select(group => new { Code = group.Key, Count = group.Select(attribute => attribute.EntityId).Distinct().Count() })
            .ToListAsync(cancellationToken);
        var mainProductDistribution = mainProductCounts
            .Where(item => !string.IsNullOrWhiteSpace(item.Code))
            .Select(group => new CrmDashboardChartItemDto
            {
                Code = group.Code,
                Name = FormatMainProduct(group.Code),
                Value = group.Count
            })
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Name)
            .ToList();

        var trendRecords = await (
                from record in _dbContext.CrmFollowRecords
                join subject in subjects on record.EntityId equals subject.Id
                where !record.IsDeleted &&
                    record.EntityType == CrmCodes.HerbBaseSubjectEntityType &&
                    record.CreatedAt >= trendStart &&
                    record.CreatedAt < tomorrowStart
                select new { record.CreatedAt, record.FollowResult })
            .ToListAsync(cancellationToken);
        var followTrend = Enumerable.Range(0, 7)
            .Select(offset => trendStart.AddDays(offset))
            .Select(date =>
            {
                var nextDate = date.AddDays(1);
                var records = trendRecords.Where(record => record.CreatedAt >= date && record.CreatedAt < nextDate).ToList();
                return new CrmDashboardTrendItemDto
                {
                    Date = date,
                    FollowCount = records.Count,
                    EffectiveFollowCount = records.Count(record => EffectiveFollowResults.Contains(record.FollowResult))
                };
            })
            .ToList();

        var newBaseDates = await _dbContext.CrmHerbBases
            .Where(herbBase =>
                herbBase.HerbBaseSubjectId.HasValue &&
                subjects.Select(subject => subject.Id).Contains(herbBase.HerbBaseSubjectId.Value) &&
                herbBase.CreatedAt >= trendStart &&
                herbBase.CreatedAt < tomorrowStart)
            .Select(herbBase => herbBase.CreatedAt)
            .ToListAsync(cancellationToken);
        var newBaseTrend = Enumerable.Range(0, 7)
            .Select(offset => trendStart.AddDays(offset))
            .Select(date =>
            {
                var nextDate = date.AddDays(1);
                return new CrmDashboardNewBaseTrendItemDto
                {
                    Date = date,
                    NewBaseCount = newBaseDates.Count(createdAt => createdAt >= date && createdAt < nextDate)
                };
            })
            .ToList();

        var vendorPriorityCounts = await vendors
            .GroupBy(vendor => vendor.PriorityLevel)
            .Select(group => new { PriorityLevel = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);
        var vendorPriorityDistribution = VendorPriorities
            .Select(priority => new CrmDashboardChartItemDto
            {
                Code = priority.Code,
                Name = priority.Name,
                Value = vendorPriorityCounts.FirstOrDefault(item => item.PriorityLevel == priority.Code)?.Count ?? 0
            })
            .ToList();

        var vendorTrendRecords = await (
                from record in _dbContext.CrmFollowRecords
                join vendor in vendors on record.EntityId equals vendor.Id
                where !record.IsDeleted &&
                    record.EntityType == CrmCodes.VendorEntityType &&
                    record.CreatedAt >= trendStart &&
                    record.CreatedAt < tomorrowStart
                select new { record.CreatedAt, record.FollowResult })
            .ToListAsync(cancellationToken);
        var vendorFollowTrend = Enumerable.Range(0, 7)
            .Select(offset => trendStart.AddDays(offset))
            .Select(date =>
            {
                var nextDate = date.AddDays(1);
                var records = vendorTrendRecords.Where(record => record.CreatedAt >= date && record.CreatedAt < nextDate).ToList();
                return new CrmDashboardTrendItemDto
                {
                    Date = date,
                    FollowCount = records.Count,
                    EffectiveFollowCount = records.Count(record => EffectiveFollowResults.Contains(record.FollowResult))
                };
            })
            .ToList();

        var newPurchaseDemandDates = await (
                from purchaseDemand in _dbContext.CrmPurchaseDemands
                join vendor in vendors on purchaseDemand.VendorId equals vendor.Id
                where !purchaseDemand.IsDeleted &&
                    purchaseDemand.CreatedAt >= trendStart &&
                    purchaseDemand.CreatedAt < tomorrowStart
                select purchaseDemand.CreatedAt)
            .ToListAsync(cancellationToken);
        var newPurchaseDemandTrend = Enumerable.Range(0, 7)
            .Select(offset => trendStart.AddDays(offset))
            .Select(date =>
            {
                var nextDate = date.AddDays(1);
                return new CrmDashboardNewPurchaseDemandTrendItemDto
                {
                    Date = date,
                    NewPurchaseDemandCount = newPurchaseDemandDates.Count(createdAt => createdAt >= date && createdAt < nextDate)
                };
            })
            .ToList();

        var vendorIds = await vendors
            .Select(vendor => vendor.Id)
            .ToListAsync(cancellationToken);
        var purchaseProductCounts = await (
                from purchaseDemand in _dbContext.CrmPurchaseDemands
                join item in _dbContext.CrmPurchaseDemandItems on purchaseDemand.Id equals item.PurchaseDemandId
                where !purchaseDemand.IsDeleted && vendorIds.Contains(purchaseDemand.VendorId)
                group purchaseDemand by item.ProductName into productGroup
                select new { Code = productGroup.Key, Count = productGroup.Select(purchaseDemand => purchaseDemand.VendorId).Distinct().Count() })
            .ToListAsync(cancellationToken);
        var vendorPurchaseProductDistribution = purchaseProductCounts
            .Where(item => !string.IsNullOrWhiteSpace(item.Code))
            .Select(group => new CrmDashboardChartItemDto
            {
                Code = group.Code,
                Name = FormatMainProduct(group.Code),
                Value = group.Count
            })
            .OrderByDescending(item => item.Value)
            .ThenBy(item => item.Name)
            .ToList();

        await FillSubjectSummariesAsync(todayFollowSubjects, cancellationToken);

        return new CrmDashboardDto
        {
            Metrics = new CrmDashboardMetricsDto
            {
                TodayFollowCount = todayFollowCount,
                OverdueFollowCount = overdueFollowCount,
                MySubjectCount = mySubjectCount,
                HighIntentSubjectCount = highIntentSubjectCount
            },
            TodayFollowSubjects = todayFollowSubjects,
            RecentFollowRecords = recentFollowRecords,
            FollowFunnel = followFunnel,
            MainProductDistribution = mainProductDistribution,
            FollowTrend = followTrend,
            NewBaseTrend = newBaseTrend,
            VendorPriorityDistribution = vendorPriorityDistribution,
            VendorFollowTrend = vendorFollowTrend,
            NewPurchaseDemandTrend = newPurchaseDemandTrend,
            VendorPurchaseProductDistribution = vendorPurchaseProductDistribution
        };
    }

    private static CrmDashboardDto BuildEmptyDashboard()
    {
        return new CrmDashboardDto
        {
            FollowFunnel =
            [
                new() { Code = CrmCodes.Status.Pending, Name = "待联系", Value = 0 },
                new() { Code = CrmCodes.Status.Following, Name = "跟进中", Value = 0 },
                new() { Code = CrmCodes.Status.Interested, Name = "有意向", Value = 0 },
                new() { Code = CrmCodes.Status.Deal, Name = "成交", Value = 0 },
                new() { Code = CrmCodes.Status.Lost, Name = "流失", Value = 0 }
            ],
            FollowTrend = Enumerable.Range(0, 7)
                .Select(offset => DateTime.Today.AddDays(-6 + offset))
                .Select(date => new CrmDashboardTrendItemDto { Date = date })
                .ToList(),
            NewBaseTrend = Enumerable.Range(0, 7)
                .Select(offset => DateTime.Today.AddDays(-6 + offset))
                .Select(date => new CrmDashboardNewBaseTrendItemDto { Date = date })
                .ToList(),
            VendorPriorityDistribution = VendorPriorities
                .Select(priority => new CrmDashboardChartItemDto { Code = priority.Code, Name = priority.Name })
                .ToList(),
            VendorFollowTrend = Enumerable.Range(0, 7)
                .Select(offset => DateTime.Today.AddDays(-6 + offset))
                .Select(date => new CrmDashboardTrendItemDto { Date = date })
                .ToList(),
            NewPurchaseDemandTrend = Enumerable.Range(0, 7)
                .Select(offset => DateTime.Today.AddDays(-6 + offset))
                .Select(date => new CrmDashboardNewPurchaseDemandTrendItemDto { Date = date })
                .ToList()
        };
    }

    private async Task FillSubjectSummariesAsync(List<CrmDashboardFollowSubjectDto> subjects, CancellationToken cancellationToken)
    {
        var subjectIds = subjects.Select(subject => subject.Id).ToList();
        if (subjectIds.Count == 0)
        {
            return;
        }

        var bases = await _dbContext.CrmHerbBases
            .Where(herbBase => herbBase.HerbBaseSubjectId.HasValue && subjectIds.Contains(herbBase.HerbBaseSubjectId.Value))
            .Select(herbBase => new
            {
                herbBase.Id,
                SubjectId = herbBase.HerbBaseSubjectId!.Value,
                herbBase.Province,
                herbBase.City,
                herbBase.Area
            })
            .ToListAsync(cancellationToken);
        var baseIds = bases.Select(herbBase => herbBase.Id).ToList();
        var attributes = await _dbContext.CrmBusinessEntityAttributes
            .Where(attribute =>
                !attribute.IsDeleted &&
                baseIds.Contains(attribute.EntityId) &&
                attribute.EntityType == CrmCodes.HerbBaseEntityType &&
                attribute.AttributeCode == CrmCodes.MainProductAttributeCode)
            .OrderBy(attribute => attribute.SortOrder)
            .ThenBy(attribute => attribute.CreatedAt)
            .Select(attribute => new { attribute.EntityId, attribute.AttributeValue })
            .ToListAsync(cancellationToken);
        var productsByBase = attributes
            .GroupBy(attribute => attribute.EntityId)
            .ToDictionary(group => group.Key, group => group.Select(attribute => attribute.AttributeValue).Distinct().ToList());

        foreach (var subject in subjects)
        {
            var subjectBases = bases.Where(herbBase => herbBase.SubjectId == subject.Id).ToList();
            subject.MainProducts = subjectBases
                .SelectMany(herbBase => productsByBase.GetValueOrDefault(herbBase.Id, []))
                .Distinct()
                .ToList();
            subject.Regions = subjectBases
                .Select(herbBase => string.Join(' ', new[] { herbBase.Province, herbBase.City, herbBase.Area }.Where(value => !string.IsNullOrWhiteSpace(value))))
                .Where(region => !string.IsNullOrWhiteSpace(region))
                .Distinct()
                .ToList();
        }
    }

    private static string FormatMainProduct(string code)
    {
        return code switch
        {
            "HUANG_QI" => "黄芪",
            "DANG_GUI" => "当归",
            "DANG_SHEN" => "党参",
            "TIAN_MA" => "天麻",
            "OTHER" => "其他",
            _ => code
        };
    }
}
