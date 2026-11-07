using QPS.Mall.Application.Features.MallPromotions;
using QPS.Mall.Domain.Entities;
using QPS.UnitTests.Common;
using Xunit;

namespace QPS.UnitTests.Application.Features.MallPromotions;

/// <summary>
/// 促销时间窗口：左闭右开 [StartDate, EndDate)，统一使用服务器本地时间。
/// </summary>
public class MallPromotionEffectiveQueryTests
{
    [Fact]
    public void WhereEffective_StartTimeIsInclusiveEndTimeIsExclusive()
    {
        using var context = TestDbContextFactory.Create();
        var now = new DateTime(2026, 3, 1, 12, 0, 0);
        var startsNow = Create("起始时刻命中", now, now.AddHours(1));
        var endsNow = Create("结束时刻不命中", now.AddHours(-1), now);
        var noWindow = Create("不限时间", null, null);
        context.MallPromotions.AddRange(startsNow, endsNow, noWindow);
        context.SaveChanges();

        var ids = context.MallPromotions
            .WhereEffective(now)
            .Select(promotion => promotion.Id)
            .ToList();

        Assert.Contains(startsNow.Id, ids);
        Assert.DoesNotContain(endsNow.Id, ids);
        Assert.Contains(noWindow.Id, ids);
    }

    [Fact]
    public void WhereEffective_ExcludesInactivePromotions()
    {
        using var context = TestDbContextFactory.Create();
        var now = new DateTime(2026, 3, 1, 12, 0, 0);
        var inactive = MallPromotion.Create(
            "已停用",
            string.Empty,
            MallPromotionType.DiscountRate,
            0m,
            "DiscountRate",
            """{"rate":0.8}""",
            "Global",
            null,
            null,
            null,
            false);
        context.MallPromotions.Add(inactive);
        context.SaveChanges();

        Assert.Empty(context.MallPromotions.WhereEffective(now).ToList());
    }

    private static MallPromotion Create(string name, DateTime? startDate, DateTime? endDate)
    {
        return MallPromotion.Create(
            name,
            string.Empty,
            MallPromotionType.DiscountRate,
            0m,
            "DiscountRate",
            """{"rate":0.8}""",
            "Global",
            null,
            startDate,
            endDate,
            true);
    }
}
