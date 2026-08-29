using Microsoft.EntityFrameworkCore;
using QPS.Application.Features.Crm;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;
using QPS.Infrastructure.Database;
using Xunit;

namespace 主页厂商图表测试;

public class 主页厂商图表测试
{
    [Fact]
    public async Task 当前负责人厂商应进入全部厂商图表()
    {
        var ownerUserId = Guid.NewGuid();
        await using var dbContext = CreateDbContext(ownerUserId);
        var vendor = CrmVendor.Create("测试厂商", "测试厂商", "High", null, string.Empty, string.Empty, ownerUserId);
        dbContext.CrmVendors.Add(vendor);
        dbContext.CrmFollowRecords.Add(CrmFollowRecord.Create(
            CrmCodes.VendorEntityType, vendor.Id, null, "PHONE", CrmCodes.FollowResult.Connected, string.Empty, string.Empty, null, ownerUserId));
        dbContext.CrmBusinessEntityAttributes.Add(new CrmBusinessEntityAttribute(
            CrmCodes.VendorEntityType, vendor.Id, "PURCHASE_PRODUCT", "HUANG_QI"));
        dbContext.CrmVendorPurchasePlans.Add(CrmVendorPurchasePlan.Create(
            vendor.Id, "测试采购计划", DateTime.Now, "黄芪", string.Empty, string.Empty));
        await dbContext.SaveChangesAsync();

        var result = await new GetCrmDashboardHandler(dbContext, new TestCurrentUserService(ownerUserId.ToString()))
            .Handle(new GetCrmDashboardQuery(), CancellationToken.None);

        Assert.Equal(1, result.VendorPriorityDistribution.Single(item => item.Code == "High").Value);
        Assert.Equal(1, result.VendorFollowTrend.Sum(item => item.FollowCount));
        Assert.Equal(1, result.NewPurchasePlanTrend.Sum(item => item.NewPurchasePlanCount));
        Assert.Equal(1, result.VendorPurchaseProductDistribution.Single(item => item.Code == "HUANG_QI").Value);
    }

    private static AppDbContext CreateDbContext(Guid ownerUserId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options, new TestCurrentUserService(ownerUserId.ToString()));
    }

    private sealed class TestCurrentUserService(string userId) : ICurrentUserService
    {
        public string? UserId { get; } = userId;
        public string? Username { get; } = "test";
    }
}
