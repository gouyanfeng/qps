using Microsoft.EntityFrameworkCore;
using QPS.Crm.Application.Features.CrmMatchOpportunities;
using QPS.Crm.Domain.Entities;
using QPS.Persistence.Database;
using QPS.UnitTests.Common;
using Xunit;

namespace QPS.UnitTests.Features.Crm.CrmMatchOpportunities;

/// <summary>撮合成功关闭时，数量差额自动生成采购明细或供应信息。</summary>
public class CrmMatchOpportunityBalanceTests
{
    [Fact]
    public async Task CloseMatched_WhenSupplyGreaterThanDemand_ShouldCreateLeftoverSupply()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var seed = await SeedAsync(dbContext, demandQuantity: 200m, supplyQuantity: 700m);

        await CloseMatchedAsync(dbContext, seed.Opportunity.Id);

        var original = await dbContext.CrmHerbBaseSupplies.SingleAsync(item => item.Id == seed.SupplyId);
        Assert.Equal(CrmHerbBaseSupply.SoldOut, original.Status);

        var leftover = await dbContext.CrmHerbBaseSupplies
            .SingleAsync(item => item.Id != seed.SupplyId && item.HerbBaseId == seed.HerbBaseId);
        Assert.Equal(500m, leftover.AvailableQuantity);
        Assert.Equal(CrmHerbBaseSupply.Active, leftover.Status);
        Assert.Equal("黄芪", leftover.ProductName);

        Assert.Equal(1, await dbContext.CrmVendorDemandItems.CountAsync(item => item.VendorDemandId == seed.DemandId));
        Assert.True(await dbContext.CrmBusinessActivities.AnyAsync(activity => activity.ActionType == "生成差额明细"));
    }

    [Fact]
    public async Task CloseMatched_WhenDemandGreaterThanSupply_ShouldCreateLeftoverDemandItem()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var seed = await SeedAsync(dbContext, demandQuantity: 400m, supplyQuantity: 300m);

        await CloseMatchedAsync(dbContext, seed.Opportunity.Id);

        var items = await dbContext.CrmVendorDemandItems
            .Where(item => item.VendorDemandId == seed.DemandId)
            .OrderBy(item => item.SortOrder)
            .ToListAsync();
        Assert.Equal(2, items.Count);
        // 新模型：原明细保留成交量（300），空闲余量（100）另起一条
        Assert.Equal(300m, items[0].Quantity);
        Assert.Equal(100m, items[1].Quantity);
        Assert.Equal("黄芪", items[1].ProductName);
        Assert.True(items[1].SortOrder > items[0].SortOrder);

        var supply = await dbContext.CrmHerbBaseSupplies.SingleAsync(item => item.Id == seed.SupplyId);
        Assert.Equal(CrmHerbBaseSupply.SoldOut, supply.Status);
        Assert.Equal(1, await dbContext.CrmHerbBaseSupplies.CountAsync(item => item.HerbBaseId == seed.HerbBaseId));
    }

    [Fact]
    public async Task CloseMatched_WhenSupplyIsInitiator_ShouldCreateLeftoverDemandItem()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var seed = await SeedAsync(dbContext, demandQuantity: 400m, supplyQuantity: 300m, source: "supply");

        await CloseMatchedAsync(dbContext, seed.Opportunity.Id);

        var items = await dbContext.CrmVendorDemandItems
            .Where(item => item.VendorDemandId == seed.DemandId)
            .OrderBy(item => item.SortOrder)
            .ToListAsync();
        Assert.Equal(2, items.Count);
        Assert.Equal(100m, items[1].Quantity);

        var supply = await dbContext.CrmHerbBaseSupplies.SingleAsync(item => item.Id == seed.SupplyId);
        Assert.Equal(CrmHerbBaseSupply.SoldOut, supply.Status);
        Assert.Equal(1, await dbContext.CrmHerbBaseSupplies.CountAsync(item => item.HerbBaseId == seed.HerbBaseId));
    }

    [Fact]
    public async Task CloseMatched_WhenQuantitiesMatch_ShouldNotCreateBalanceRecords()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var seed = await SeedAsync(dbContext, demandQuantity: 300m, supplyQuantity: 300m);

        await CloseMatchedAsync(dbContext, seed.Opportunity.Id);

        Assert.Equal(1, await dbContext.CrmVendorDemandItems.CountAsync(item => item.VendorDemandId == seed.DemandId));
        Assert.Equal(1, await dbContext.CrmHerbBaseSupplies.CountAsync(item => item.HerbBaseId == seed.HerbBaseId));
        Assert.False(await dbContext.CrmBusinessActivities.AnyAsync(activity => activity.ActionType == "生成差额明细"));
    }

    private static async Task CloseMatchedAsync(AppDbContext dbContext, Guid opportunityId)
    {
        var handler = new UpdateCrmMatchOpportunityStatusHandler(dbContext);
        var result = await handler.Handle(new UpdateCrmMatchOpportunityStatusCommand
        {
            OpportunityId = opportunityId,
            Status = CrmMatchOpportunity.Closed,
            CloseResult = CrmMatchOpportunity.Matched
        }, CancellationToken.None);

        Assert.True(result);
    }

    private static async Task<(Guid DemandId, Guid SupplyId, Guid HerbBaseId, CrmMatchOpportunity Opportunity)> SeedAsync(
        AppDbContext dbContext,
        decimal demandQuantity,
        decimal supplyQuantity,
        string source = "demand")
    {
        var vendor = CrmVendor.Create("测试厂商", "测试厂商", "高", string.Empty);
        var demandItem = new CrmVendorDemandItem(
            "黄芪", demandQuantity, "公斤", "统货", "无硫", 30m, "元/公斤", DateTime.Today.AddDays(30), string.Empty, 1);
        var demand = CrmVendorDemand.Create(
            vendor.Id, "TEST-001", "黄芪需求", DateTime.Today, "手工录入", null,
            DateTime.Today.AddDays(30), "测试收货地址", string.Empty, string.Empty, [demandItem]);

        var herbBase = CrmHerbBase.Create(
            "测试基地", "A", 90, "甘肃省", "定西市", "陇西县", "测试基地地址", null, null, "手工录入", null, null, string.Empty);
        var supply = CrmHerbBaseSupply.Create(
            herbBase.Id, null, "黄芪", supplyQuantity, "公斤", "统货", "无硫", "秋季", 30m, "元/公斤",
            "测试周期", DateTime.Today, DateTime.Today.AddDays(60), string.Empty);
        supply.ChangeStatus(CrmHerbBaseSupply.Active);

        dbContext.CrmVendors.Add(vendor);
        dbContext.CrmVendorDemands.Add(demand);
        dbContext.CrmHerbBases.Add(herbBase);
        dbContext.CrmHerbBaseSupplies.Add(supply);
        await dbContext.SaveChangesAsync();

        var opportunity = CrmMatchOpportunity.Create(
            null,
            "核实数量",
            DateTime.Today.AddDays(3),
            string.Empty,
            source);
        dbContext.CrmMatchOpportunities.Add(opportunity);

        // 发起方与候选统一落在明细子表：只存 EntityType + EntityId，数量等业务字段实时取原明细，不再存快照。
        // 需求发起：采购明细为发起方，供应信息为中选候选；供应发起则相反。
        var initiatorItem = CrmMatchOpportunityItem.Create(
            opportunity.Id,
            source == "supply" ? CrmMatchOpportunityItem.EntityTypeSupply : CrmMatchOpportunityItem.EntityTypeDemand,
            source == "supply" ? supply.Id : demandItem.Id,
            CrmMatchOpportunityItem.Initiator,
            1);
        dbContext.CrmMatchOpportunityItems.Add(initiatorItem);

        var candidateItem = CrmMatchOpportunityItem.Create(
            opportunity.Id,
            source == "supply" ? CrmMatchOpportunityItem.EntityTypeDemand : CrmMatchOpportunityItem.EntityTypeSupply,
            source == "supply" ? demandItem.Id : supply.Id,
            CrmMatchOpportunityItem.Candidate,
            1);
        candidateItem.Select();
        dbContext.CrmMatchOpportunityItems.Add(candidateItem);

        await dbContext.SaveChangesAsync();

        // 撮合成功仅允许在意向确认阶段关闭
        opportunity.Advance(CrmMatchOpportunityStage.BusinessNegotiation, "沟通价格", string.Empty, DateTime.Today.AddDays(3));
        opportunity.Advance(CrmMatchOpportunityStage.IntentConfirmation, "确认中选供应", string.Empty, DateTime.Today.AddDays(3));
        await dbContext.SaveChangesAsync();

        return (demand.Id, supply.Id, herbBase.Id, opportunity);
    }
}
