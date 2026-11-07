using Microsoft.EntityFrameworkCore;
using QPS.Persistence.Database;
using QPS.Shared.Contracts;
using Xunit;

namespace QPS.ArchitectureTests;

/// <summary>
/// 唯一索引必须带软删除过滤：软删除后同一业务键可以重新创建，
/// 若唯一索引不过滤 IsDeleted，历史软删记录会阻止新数据写入。
/// 通过 EF 模型（AppDbContext.Model）扫描，覆盖所有 HasIndex(...).IsUnique() 配置。
/// </summary>
public class UniqueIndexTests
{
    private const string SoftDeleteFilterToken = "[IsDeleted]";

    /// <summary>
    /// 显式例外白名单：键为 "表名.索引名"，值为豁免原因。
    /// 当前没有需要豁免唯一索引的实体；新增例外必须在此登记并说明原因。
    /// </summary>
    private static readonly Dictionary<string, string> Exemptions = new(StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void UniqueIndexes_ShouldIncludeSoftDeleteFilter()
    {
        using var context = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=localhost;Database=QPS_ArchitectureTests_Model;Trusted_Connection=True;")
                .Options,
            new StubCurrentUserService());

        var violations = new List<string>();

        foreach (var entityType in context.Model.GetEntityTypes())
        {
            foreach (var index in entityType.GetIndexes().Where(index => index.IsUnique))
            {
                var table = entityType.GetTableName() ?? entityType.ClrType.Name;
                var indexName = index.GetDatabaseName()
                    ?? string.Join("_", index.Properties.Select(property => property.GetColumnName()));
                var key = $"{table}.{indexName}";
                if (Exemptions.ContainsKey(key))
                {
                    continue;
                }

                var filter = index.GetFilter();
                if (filter is null || !filter.Contains(SoftDeleteFilterToken, StringComparison.Ordinal))
                {
                    violations.Add(
                        $"{table}.{indexName}（实体 {entityType.ClrType.Name}）唯一索引过滤条件为 " +
                        $"{filter ?? "<null>"}，缺少 {SoftDeleteFilterToken}");
                }
            }
        }

        Assert.Empty(violations);
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public string? UserId => null;

        public string? Username => null;
    }
}
