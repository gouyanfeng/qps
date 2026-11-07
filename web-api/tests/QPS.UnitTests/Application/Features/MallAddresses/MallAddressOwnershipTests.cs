using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallAddresses;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;
using QPS.UnitTests.Common;
using Xunit;

namespace QPS.UnitTests.Application.Features.MallAddresses;

/// <summary>收货地址按当前会员收口：列表只返回本人，跨会员读改删按 404 不存在处理。</summary>
public class MallAddressOwnershipTests
{
    [Fact]
    public async Task Addresses_AreScopedToOwner()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;
        var memberA = Guid.NewGuid();
        var memberB = Guid.NewGuid();
        var addressA = MallAddress.Create(memberA, "甲", "13800000001", "省", "市", "区", "A 地址", true);
        var addressB = MallAddress.Create(memberB, "乙", "13800000002", "省", "市", "区", "B 地址", false);
        db.MallAddresses.AddRange(addressA, addressB);
        await db.SaveChangesAsync();

        var list = await new GetMallAddressesHandler(db).Handle(
            new GetMallAddressesQuery { UserId = memberA }, CancellationToken.None);
        Assert.Single(list);
        Assert.Equal(addressA.Id, list[0].Id);

        var notFound = await Assert.ThrowsAsync<BusinessException>(() =>
            new GetMallAddressHandler(db).Handle(
                new GetMallAddressQuery { AddressId = addressB.Id, UserId = memberA }, CancellationToken.None));
        Assert.Equal(404, notFound.ErrorCode);

        notFound = await Assert.ThrowsAsync<BusinessException>(() =>
            new UpdateMallAddressHandler(db).Handle(
                new UpdateMallAddressCommand
                {
                    AddressId = addressB.Id,
                    UserId = memberA,
                    Request = new MallAddressUpdateRequest
                    {
                        ReceiverName = "丙",
                        ReceiverPhone = "13800000003",
                        ProvinceName = "省",
                        CityName = "市",
                        DistrictName = "区",
                        DetailAddress = "篡改地址",
                        IsDefault = true
                    }
                },
                CancellationToken.None));
        Assert.Equal(404, notFound.ErrorCode);

        notFound = await Assert.ThrowsAsync<BusinessException>(() =>
            new DeleteMallAddressHandler(db).Handle(
                new DeleteMallAddressCommand { AddressId = addressB.Id, UserId = memberA }, CancellationToken.None));
        Assert.Equal(404, notFound.ErrorCode);

        var untouched = await db.MallAddresses.AsNoTracking().FirstAsync(a => a.Id == addressB.Id);
        Assert.Equal("乙", untouched.ReceiverName);
    }
}
