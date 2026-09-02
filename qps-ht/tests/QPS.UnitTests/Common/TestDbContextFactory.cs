using Microsoft.EntityFrameworkCore;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.System;
using QPS.Infrastructure.Database;

namespace QPS.UnitTests.Common;

internal static class TestDbContextFactory
{
    public static AppDbContext Create(ICurrentUserService? currentUserService = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options, currentUserService ?? new TestCurrentUserService());
        var rootId = Guid.NewGuid();
        context.SystemDataDictionaries.AddRange(
            new SystemDataDictionary(rootId, "CRM_HERB_PRODUCT", "中药材品类", "中药材品类", "测试品类根节点", 0, true),
            new SystemDataDictionary(Guid.NewGuid(), "HUANG_QI", "HUANG_QI", "黄芪", "", 1, true, rootId),
            new SystemDataDictionary(Guid.NewGuid(), "DANG_GUI", "DANG_GUI", "当归", "", 2, true, rootId),
            new SystemDataDictionary(Guid.NewGuid(), "TIAN_MA", "TIAN_MA", "天麻", "", 3, true, rootId));
        context.SaveChanges();
        return context;
    }

    public static IDomainEventDispatcher CreateDispatcher()
    {
        return new TestDomainEventDispatcher();
    }
}

internal sealed class TestCurrentUserService : ICurrentUserService
{
    public TestCurrentUserService(string? userId = "unit-test", string? username = "unit-test")
    {
        UserId = userId;
        Username = username;
    }

    public string? UserId { get; }

    public string? Username { get; }
}
