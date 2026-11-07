using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using QPS.Persistence.Database;

namespace QPS.UnitTests.Common;

/// <summary>
/// 退款、库存回补与券退还需要 ExecuteUpdateAsync，InMemory 提供程序不支持，
/// 退款链路测试使用 SQLite 内存库执行真实 Handler。
/// </summary>
internal sealed class TestSqliteDbContextFactory : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private TestSqliteDbContextFactory(SqliteConnection connection, AppDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public AppDbContext Context { get; }

    /// <summary>
    /// 在同一个 SQLite 内存库上创建另一个 DbContext，用于模拟并发请求的第二个连接。
    /// </summary>
    public AppDbContext CreateContext()
    {
        var options = BuildOptions(_connection);
        return new AppDbContext(options, new TestCurrentUserService());
    }

    public static async Task<TestSqliteDbContextFactory> CreateAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var context = new AppDbContext(BuildOptions(connection), new TestCurrentUserService());
        await context.Database.EnsureCreatedAsync();

        return new TestSqliteDbContextFactory(connection, context);
    }

    private static DbContextOptions<AppDbContext> BuildOptions(SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .ReplaceService<IModelCustomizer, SqliteModelCustomizer>()
            .Options;
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

/// <summary>
/// 测试模型去掉 SQLite 不支持的 SQL Server 写法：带括号的列类型（如 nvarchar(max)）和 N'...' 筛选索引。
/// </summary>
internal sealed class SqliteModelCustomizer : ModelCustomizer
{
    public SqliteModelCustomizer(ModelCustomizerDependencies dependencies) : base(dependencies)
    {
    }

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var columnType = property.GetColumnType();
                if (columnType is not null && columnType.Contains('('))
                {
                    property.SetColumnType(null);
                }
            }

            foreach (var index in entityType.GetIndexes())
            {
                index.RemoveAnnotation("Relational:Filter");
            }
        }
    }
}
