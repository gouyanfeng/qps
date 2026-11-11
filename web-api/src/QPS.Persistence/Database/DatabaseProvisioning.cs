using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace QPS.Persistence.Database;

/// <summary>
/// 数据库供应：启动时判断目标库是否存在，缺失则由程序自动建库并写入 System 基线数据。
/// </summary>
public static class DatabaseProvisioning
{
    /// <summary>
    /// 检查目标数据库是否已在服务器上存在。连接 master 查询，不依赖目标库本身能否登录。
    /// 服务器不可达时抛出异常，由调用方决定如何处理。
    /// </summary>
    public static async Task<bool> DatabaseExistsAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            return false;
        }

        var masterConnectionString = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString;

        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM sys.databases WHERE name = @databaseName";
        command.Parameters.AddWithValue("@databaseName", databaseName);

        var count = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(count) > 0;
    }

    /// <summary>
    /// 为「迁移压扁前就存在的旧库」补记 InitialCreate 迁移历史（幂等）。
    /// 旧库的 __EFMigrationsHistory 里只有压扁前的迁移记录，若不补记，
    /// 后续新增增量迁移执行 dotnet ef database update 时会重复执行 InitialCreate 而建表失败。
    /// </summary>
    public static async Task MarkInitialMigrationAppliedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var initialMigrationId = dbContext.Database.GetMigrations().FirstOrDefault();
        if (initialMigrationId is null)
        {
            return;
        }

        var productVersion = typeof(DbContext).Assembly.GetName().Version?.ToString(3) ?? "8.0.0";

        const string sql = """
            IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NULL RETURN;
            IF EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory])
               AND NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = {0})
            BEGIN
                INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({0}, {1});
            END
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, new object[] { initialMigrationId, productVersion }, cancellationToken);
    }
}
