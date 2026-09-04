using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Contracts.Crm.CrmContacts;
using QPS.Application.Features.Crm;
using QPS.Application.Features.Crm.CrmContacts;
using QPS.Domain.Entities.Crm;
using QPS.Domain.Exceptions;
using QPS.UnitTests.Common;
using Xunit;

namespace QPS.UnitTests.Features.Crm.CrmContacts;

public class CrmContactCommandTests
{
    [Fact]
    public async Task Create_ShouldPromoteFirstContactToPrimary_WhenCustomerHasNoPrimaryContact()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var subject = CreateSubject();
        dbContext.CrmHerbBaseSubjects.Add(subject);
        await dbContext.SaveChangesAsync();

        var handler = new CreateCrmContactHandler(dbContext, TestDbContextFactory.CreateDispatcher());

        var result = await handler.Handle(new CreateCrmContactCommand
        {
            EntityType = CrmCodes.HerbBaseSubjectEntityType,
            EntityId = subject.Id,
            Request = new CrmContactCreateRequest
            {
                ContactName = "First Contact",
                Phone = "13800000001",
                PhoneType = "MOBILE",
                RoleName = "OWNER",
                IsPrimary = false
            }
        }, CancellationToken.None);

        var persistedSubject = await dbContext.CrmHerbBaseSubjects.SingleAsync(item => item.Id == subject.Id);
        var persistedContact = await dbContext.CrmContacts.SingleAsync();
        Assert.True(result);
        Assert.True(persistedContact.IsPrimary);
        Assert.Equal("First Contact", persistedSubject.PrimaryContactName);
        Assert.Equal("13800000001", persistedSubject.PrimaryContactPhone);
    }

    [Fact]
    public async Task Create_ShouldPromoteFirstVendorContactToPrimary()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var vendor = CreateVendor();
        dbContext.CrmVendors.Add(vendor);
        await dbContext.SaveChangesAsync();

        var handler = new CreateCrmContactHandler(dbContext, TestDbContextFactory.CreateDispatcher());

        var result = await handler.Handle(new CreateCrmContactCommand
        {
            EntityType = CrmCodes.VendorEntityType,
            EntityId = vendor.Id,
            Request = new CrmContactCreateRequest
            {
                ContactName = "Vendor Contact",
                Phone = "13800000003",
                PhoneType = "MOBILE",
                RoleName = "PURCHASE",
                IsPrimary = false
            }
        }, CancellationToken.None);

        var persistedContact = await dbContext.CrmContacts.SingleAsync();
        Assert.True(result);
        Assert.True(persistedContact.IsPrimary);
        Assert.Equal(CrmCodes.VendorEntityType, persistedContact.EntityType);
        Assert.Equal(vendor.Id, persistedContact.EntityId);
    }

    [Fact]
    public async Task Update_ShouldUpdateVendorContactAndPrimary()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var vendor = CreateVendor();
        var primary = CreateVendorContact(vendor.Id, "Primary Vendor Contact", "13800000004", true);
        var replacement = CreateVendorContact(vendor.Id, "Replacement Vendor Contact", "13800000005", false);
        dbContext.CrmVendors.Add(vendor);
        dbContext.CrmContacts.AddRange(primary, replacement);
        await dbContext.SaveChangesAsync();

        var handler = new UpdateCrmContactHandler(dbContext, TestDbContextFactory.CreateDispatcher());

        var result = await handler.Handle(new UpdateCrmContactCommand
        {
            EntityType = CrmCodes.VendorEntityType,
            EntityId = vendor.Id,
            Id = replacement.Id,
            Request = new CrmContactUpdateRequest
            {
                ContactName = "Updated Vendor Contact",
                Phone = "13800000006",
                PhoneType = "MOBILE",
                RoleName = "PURCHASE",
                IsPrimary = true
            }
        }, CancellationToken.None);

        Assert.True(result);
        Assert.False(primary.IsPrimary);
        Assert.True(replacement.IsPrimary);
        Assert.Equal("Updated Vendor Contact", replacement.ContactName);
        Assert.Equal("13800000006", replacement.Phone);
    }

    [Fact]
    public async Task SetPrimary_ShouldRejectVendorContactOutsideTarget()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var vendor = CreateVendor();
        var otherVendor = CreateVendor("Other Vendor");
        var contact = CreateVendorContact(otherVendor.Id, "Other Vendor Contact", "13800000007", false);
        dbContext.CrmVendors.AddRange(vendor, otherVendor);
        dbContext.CrmContacts.Add(contact);
        await dbContext.SaveChangesAsync();

        var handler = new SetPrimaryCrmContactHandler(dbContext, TestDbContextFactory.CreateDispatcher());

        var exception = await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(new SetPrimaryCrmContactCommand
        {
            EntityType = CrmCodes.VendorEntityType,
            EntityId = vendor.Id,
            Id = contact.Id
        }, CancellationToken.None));

        Assert.Equal(404, exception.ErrorCode);
    }

    [Fact]
    public async Task SetPrimary_ShouldSetVendorContactPrimary()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var vendor = CreateVendor();
        var primary = CreateVendorContact(vendor.Id, "Primary Vendor Contact", "13800000008", true);
        var replacement = CreateVendorContact(vendor.Id, "Replacement Vendor Contact", "13800000009", false);
        dbContext.CrmVendors.Add(vendor);
        dbContext.CrmContacts.AddRange(primary, replacement);
        await dbContext.SaveChangesAsync();

        var handler = new SetPrimaryCrmContactHandler(dbContext, TestDbContextFactory.CreateDispatcher());

        var result = await handler.Handle(new SetPrimaryCrmContactCommand
        {
            EntityType = CrmCodes.VendorEntityType,
            EntityId = vendor.Id,
            Id = replacement.Id
        }, CancellationToken.None);

        Assert.True(result);
        Assert.False(primary.IsPrimary);
        Assert.True(replacement.IsPrimary);
    }

    [Fact]
    public async Task UpdateStatus_ShouldPromoteOldestValidContact_WhenPrimaryContactBecomesInvalid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var subject = CreateSubject();
        var primary = CrmContact.Create(
            "CRM_HERB_BASE_SUBJECT",
            subject.Id,
            "Primary Contact",
            "13800000001",
            "MOBILE",
            "",
            "OWNER",
            true,
            "");
        var replacement = CrmContact.Create(
            "CRM_HERB_BASE_SUBJECT",
            subject.Id,
            "Replacement Contact",
            "13800000002",
            "MOBILE",
            "",
            "PURCHASE",
            false,
            "");
        primary.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        replacement.CreatedAt = DateTime.UtcNow.AddMinutes(-5);
        subject.UpdatePrimaryContact(primary.ContactName, primary.Phone);
        dbContext.CrmHerbBaseSubjects.Add(subject);
        dbContext.CrmContacts.AddRange(primary, replacement);
        await dbContext.SaveChangesAsync();

        var handler = new UpdateCrmContactStatusHandler(dbContext, TestDbContextFactory.CreateDispatcher());

        await handler.Handle(new UpdateCrmContactStatusCommand
        {
            EntityType = CrmCodes.HerbBaseSubjectEntityType,
            EntityId = subject.Id,
            Id = primary.Id,
            Request = new CrmContactStatusRequest
            {
                Status = "无效",
                Remark = "Wrong number"
            }
        }, CancellationToken.None);

        Assert.False(primary.IsPrimary);
        Assert.True(replacement.IsPrimary);
        Assert.Equal("无效", primary.Status);
        Assert.Equal("Replacement Contact", subject.PrimaryContactName);
        Assert.Equal("13800000002", subject.PrimaryContactPhone);
    }

    [Fact]
    public async Task UpdateStatus_ShouldPromoteOldestValidVendorContact_WhenPrimaryContactBecomesInvalid()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var vendor = CreateVendor();
        var primary = CreateVendorContact(vendor.Id, "Primary Vendor Contact", "13800000010", true);
        var replacement = CreateVendorContact(vendor.Id, "Replacement Vendor Contact", "13800000011", false);
        primary.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        replacement.CreatedAt = DateTime.UtcNow.AddMinutes(-5);
        dbContext.CrmVendors.Add(vendor);
        dbContext.CrmContacts.AddRange(primary, replacement);
        await dbContext.SaveChangesAsync();

        var handler = new UpdateCrmContactStatusHandler(dbContext, TestDbContextFactory.CreateDispatcher());

        var result = await handler.Handle(new UpdateCrmContactStatusCommand
        {
            EntityType = CrmCodes.VendorEntityType,
            EntityId = vendor.Id,
            Id = primary.Id,
            Request = new CrmContactStatusRequest
            {
                Status = "无效",
                Remark = "Wrong number"
            }
        }, CancellationToken.None);

        Assert.True(result);
        Assert.False(primary.IsPrimary);
        Assert.True(replacement.IsPrimary);
        Assert.Equal("无效", primary.Status);
    }

    [Fact]
    public async Task Update_ShouldRejectContactOutsideTarget()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var subject = CreateSubject();
        var vendor = CreateVendor();
        var contact = CreateVendorContact(vendor.Id, "Vendor Contact", "13800000012", true);
        dbContext.CrmHerbBaseSubjects.Add(subject);
        dbContext.CrmVendors.Add(vendor);
        dbContext.CrmContacts.Add(contact);
        await dbContext.SaveChangesAsync();

        var handler = new UpdateCrmContactHandler(dbContext, TestDbContextFactory.CreateDispatcher());

        var exception = await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(new UpdateCrmContactCommand
        {
            EntityType = CrmCodes.HerbBaseSubjectEntityType,
            EntityId = subject.Id,
            Id = contact.Id,
            Request = new CrmContactUpdateRequest
            {
                ContactName = "Cross Target Contact",
                Phone = "13800000013",
                PhoneType = "MOBILE",
                IsPrimary = true
            }
        }, CancellationToken.None));

        Assert.Equal(404, exception.ErrorCode);
    }

    [Fact]
    public async Task UpdateStatus_ShouldRejectContactOutsideTarget()
    {
        await using var dbContext = TestDbContextFactory.Create();
        var vendor = CreateVendor();
        var otherVendor = CreateVendor("Other Vendor");
        var contact = CreateVendorContact(otherVendor.Id, "Other Vendor Contact", "13800000014", true);
        dbContext.CrmVendors.AddRange(vendor, otherVendor);
        dbContext.CrmContacts.Add(contact);
        await dbContext.SaveChangesAsync();

        var handler = new UpdateCrmContactStatusHandler(dbContext, TestDbContextFactory.CreateDispatcher());

        var exception = await Assert.ThrowsAsync<BusinessException>(() => handler.Handle(new UpdateCrmContactStatusCommand
        {
            EntityType = CrmCodes.VendorEntityType,
            EntityId = vendor.Id,
            Id = contact.Id,
            Request = new CrmContactStatusRequest
            {
                Status = "无效",
                Remark = "Wrong target"
            }
        }, CancellationToken.None));

        Assert.Equal(404, exception.ErrorCode);
    }

    private static CrmHerbBaseSubject CreateSubject()
        => CrmHerbBaseSubject.Create(
            "Contact Test Subject",
            "Contact Test Base",
            "UNKNOWN",
            null,
            "PENDING",
            "B",
            80,
            "Remark");

    private static CrmVendor CreateVendor(string vendorName = "Contact Test Vendor")
        => CrmVendor.Create(vendorName, vendorName.ToUpperInvariant(), "Medium", "Remark");

    private static CrmContact CreateVendorContact(Guid vendorId, string name, string phone, bool isPrimary)
        => CrmContact.Create(
            CrmCodes.VendorEntityType,
            vendorId,
            name,
            phone,
            "MOBILE",
            "",
            "PURCHASE",
            isPrimary,
            "");
}
