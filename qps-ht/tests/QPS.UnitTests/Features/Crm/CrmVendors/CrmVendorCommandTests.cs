using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Features.Crm.CrmVendors;
using QPS.Domain.Entities.System;
using QPS.UnitTests.Common;
using Xunit;

namespace QPS.UnitTests.Features.Crm.CrmVendors;

public class CrmVendorCommandTests
{
    [Fact]
    public async Task Create_ShouldAddDefaultTransferRecord()
    {
        var operatorUser = SystemUser.Create("operator", "password", "Operator", Guid.NewGuid());
        var requestedOwner = SystemUser.Create("owner", "password", "Owner", Guid.NewGuid());
        await using var dbContext = TestDbContextFactory.Create(new TestCurrentUserService(operatorUser.Id.ToString()));
        dbContext.SystemUsers.AddRange(operatorUser, requestedOwner);
        await dbContext.SaveChangesAsync();

        var handler = new CreateCrmVendorHandler(dbContext, new TestCurrentUserService(operatorUser.Id.ToString()));

        var result = await handler.Handle(new CreateCrmVendorCommand
        {
            Request = new CrmVendorCreateRequest
            {
                VendorName = "Default Transfer Vendor",
                PriorityLevel = "High",
                OwnerUserId = requestedOwner.Id,
                Remark = "Created by unit test"
            }
        }, CancellationToken.None);

        var vendor = await dbContext.CrmVendors.SingleAsync(item => item.VendorName == "Default Transfer Vendor");
        var transferRecord = await dbContext.CrmTransferRecords.SingleAsync();

        Assert.True(result);
        Assert.Equal("ENTRY", transferRecord.ActionType);
        Assert.Equal("CRM_VENDOR", transferRecord.EntityType);
        Assert.Equal(vendor.Id, transferRecord.EntityId);
        Assert.Null(transferRecord.FromOwnerUserId);
        Assert.Equal(operatorUser.Id, transferRecord.ToOwnerUserId);
        Assert.Equal(operatorUser.Id, transferRecord.OperatorUserId);
        Assert.Equal("Created by unit test", transferRecord.Remark);
    }

    [Fact]
    public async Task Get_ShouldIncludeTransferRecordsFromGenericTable()
    {
        var operatorUser = SystemUser.Create("operator", "password", "Operator", Guid.NewGuid());
        var owner = SystemUser.Create("owner", "password", "Owner", Guid.NewGuid());
        await using var dbContext = TestDbContextFactory.Create(new TestCurrentUserService(operatorUser.Id.ToString()));
        dbContext.SystemUsers.AddRange(operatorUser, owner);
        await dbContext.SaveChangesAsync();

        var createHandler = new CreateCrmVendorHandler(
            dbContext,
            new TestCurrentUserService(operatorUser.Id.ToString()));
        await createHandler.Handle(new CreateCrmVendorCommand
        {
            Request = new CrmVendorCreateRequest
            {
                VendorName = "Transfer History Vendor",
                PriorityLevel = "High",
                OwnerUserId = owner.Id,
                Remark = "Initial assignment"
            }
        }, CancellationToken.None);

        var vendor = await dbContext.CrmVendors.SingleAsync(item => item.VendorName == "Transfer History Vendor");
        var detail = await new GetCrmVendorHandler(dbContext).Handle(
            new GetCrmVendorQuery { Id = vendor.Id },
            CancellationToken.None);

        var transferRecord = Assert.Single(detail.TransferRecords);
        Assert.Equal("ENTRY", transferRecord.ActionType);
        Assert.Null(transferRecord.FromOwnerUserId);
        Assert.Equal(operatorUser.Id, transferRecord.ToOwnerUserId);
        Assert.Equal("Operator", transferRecord.ToOwnerUserName);
        Assert.Equal("Operator", transferRecord.OperatorUserName);
        Assert.Equal("Initial assignment", transferRecord.Remark);
    }
}
