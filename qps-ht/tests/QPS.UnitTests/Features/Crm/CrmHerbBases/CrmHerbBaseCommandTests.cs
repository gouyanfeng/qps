using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Features.Crm.CrmHerbBaseSubjects;
using QPS.Application.Features.Crm.CrmHerbBases;
using QPS.Domain.Entities.Crm;
using QPS.Domain.Entities.System;
using QPS.UnitTests.Common;
using Xunit;

namespace QPS.UnitTests.Features.Crm.CrmHerbBases;

public class CrmHerbBaseCommandTests
{
    [Fact]
    public async Task Create_ShouldNotCreateProductAttributesOrSupplies()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var currentUser = SystemUser.Create("creator", "hash", "创建人", Guid.NewGuid());
        dbContext.SystemUsers.Add(currentUser);
        await dbContext.SaveChangesAsync();
        var handler = new CreateCrmHerbBaseHandler(
            dbContext,
            new TestCurrentUserService(currentUser.Id.ToString()),
            TestDbContextFactory.CreateDispatcher());

        var result = await handler.Handle(new CreateCrmHerbBaseCommand
        {
            Request = new CrmHerbBaseCreateRequest
            {
                BaseName = "Multi Product Customer",
                Grade = "A",
                Score = 95,
                Province = "Gansu",
                City = "Dingxi",
                Area = "Longxi",
                Address = "Test address",
                SourcePlatform = "MANUAL",
                SourceId = 3001,
                PrimaryContactName = "Primary Contact",
                PrimaryContactPhone = "13900000000",
                Remark = "Created by unit test"
            }
        }, CancellationToken.None);

        var customer = await dbContext.CrmHerbBases.SingleAsync(item => item.BaseName == "Multi Product Customer");
        var subject = await dbContext.CrmHerbBaseSubjects.SingleAsync(item => item.Id == customer.HerbBaseSubjectId);
        var attributes = await dbContext.CrmBusinessEntityAttributes
            .Where(attribute => attribute.EntityId == customer.Id)
            .OrderBy(attribute => attribute.SortOrder)
            .Select(attribute => attribute.AttributeValue)
            .ToListAsync();
        var transferRecordCount = await dbContext.CrmTransferRecords.CountAsync();

        Assert.True(result);
        Assert.Empty(attributes);
        Assert.Empty(await dbContext.CrmHerbBaseSupplies.ToListAsync());
        Assert.Equal("Primary Contact", subject.PrimaryContactName);
        Assert.Equal("13900000000", subject.PrimaryContactPhone);
        Assert.Equal(1, transferRecordCount);
    }

    [Fact]
    public async Task GetSubjectList_ShouldAggregateSupplyProductName_AndFilterByProductName()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var matchingSubject = AddSubjectWithSupply(dbContext, "Supply Astragalus Subject", "黄芪");
        AddSubjectWithSupply(dbContext, "Supply Angelica Subject", "当归");
        await dbContext.SaveChangesAsync();

        var handler = new GetCrmHerbBaseSubjectsHandler(dbContext);

        var result = await handler.Handle(new GetCrmHerbBaseSubjectsQuery
        {
            ProductName = new List<string> { "黄芪" },
            Page = 1,
            PageSize = 10
        }, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        var subject = Assert.Single(result.List);
        Assert.Equal(matchingSubject.Id, subject.Id);
        Assert.Equal(new List<string> { "黄芪" }, subject.ProductName);
    }

    [Fact]
    public async Task Delete_ShouldHideCustomerFromList()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var customer = CrmHerbBase.Create(
            "Deleted Customer",
            "B",
            80,
            "Gansu",
            "Dingxi",
            "Longxi",
            "Test address",
            null,
            null,
            "MANUAL",
            null,
            null,
            "Remark");
        dbContext.CrmHerbBases.Add(customer);
        await dbContext.SaveChangesAsync();

        var deleteHandler = new DeleteCrmHerbBaseHandler(dbContext, TestDbContextFactory.CreateDispatcher());
        await deleteHandler.Handle(new DeleteCrmHerbBaseCommand { Id = customer.Id }, CancellationToken.None);

        var list = await new GetCrmHerbBasesHandler(dbContext).Handle(new GetCrmHerbBasesQuery
        {
            Keyword = "Deleted Customer"
        }, CancellationToken.None);

        Assert.True(customer.IsDeleted);
        Assert.Empty(list.List);
        Assert.Equal(0, list.TotalCount);
    }

    private static CrmHerbBaseSubject AddSubjectWithSupply(
        QPS.Infrastructure.Database.AppDbContext dbContext,
        string subjectName,
        string productName)
    {
        var subject = CrmHerbBaseSubject.Create(subjectName, subjectName, "仅基地", null, "待联系", "低", 0, string.Empty);
        var herbBase = CrmHerbBase.Create(subjectName, "低", 0, "甘肃省", "定西市", "陇西县", string.Empty, null, null, "手工录入", null, null, string.Empty);
        herbBase.SetHerbBaseSubject(subject.Id);
        dbContext.CrmHerbBaseSubjects.Add(subject);
        dbContext.CrmHerbBases.Add(herbBase);
        dbContext.CrmHerbBaseSupplies.Add(CrmHerbBaseSupply.Create(
            herbBase.Id, subject.Id, productName, null, string.Empty, string.Empty, string.Empty,
            string.Empty, null, string.Empty, string.Empty, null, null, string.Empty));
        return subject;
    }
}
