using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmVendorFollowSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastFollowAt",
                table: "CrmVendors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastFollowResult",
                table: "CrmVendors",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "NextFollowAt",
                table: "CrmVendors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendors_OwnerUserId_NextFollowAt",
                table: "CrmVendors",
                columns: new[] { "OwnerUserId", "NextFollowAt" });

            migrationBuilder.Sql("""
                ;WITH LatestFollowRecord AS
                (
                    SELECT
                        [EntityId],
                        [CreatedAt],
                        [FollowResult],
                        [NextFollowAt],
                        ROW_NUMBER() OVER (PARTITION BY [EntityId] ORDER BY [CreatedAt] DESC, [Id] DESC) AS [RowNumber]
                    FROM [CrmFollowRecords]
                    WHERE [EntityType] = 'CRM_VENDOR' AND [IsDeleted] = 0
                )
                UPDATE [vendor]
                SET
                    [LastFollowAt] = [record].[CreatedAt],
                    [LastFollowResult] = [record].[FollowResult],
                    [NextFollowAt] = [record].[NextFollowAt]
                FROM [CrmVendors] AS [vendor]
                INNER JOIN LatestFollowRecord AS [record]
                    ON [record].[EntityId] = [vendor].[Id] AND [record].[RowNumber] = 1
                WHERE [vendor].[LastFollowAt] IS NULL
                    AND [vendor].[LastFollowResult] = ''
                    AND [vendor].[NextFollowAt] IS NULL;
                """);

            migrationBuilder.Sql("""
                DECLARE @permissionId uniqueidentifier =
                    (SELECT TOP (1) [Id] FROM [SystemPermissions] WHERE [Code] = 'CRM_FOLLOW_TASK' AND [IsDeleted] = 0);

                IF @permissionId IS NULL
                BEGIN
                    DECLARE @rootId uniqueidentifier =
                        (SELECT TOP (1) [Id] FROM [SystemPermissions] WHERE [Code] = 'ROOT' AND [IsDeleted] = 0);

                    IF @rootId IS NULL
                        THROW 50000, '未找到 ROOT 权限，无法创建跟进任务权限。', 1;

                    SET @permissionId = NEWID();

                    INSERT INTO [SystemPermissions]
                        ([Id], [Name], [Code], [ParentId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted])
                    VALUES
                        (@permissionId, N'跟进任务', 'CRM_FOLLOW_TASK', @rootId, GETDATE(), 'migration', GETDATE(), 'migration', 0);
                END;

                INSERT INTO [SystemRolePermissions]
                    ([Id], [RoleId], [PermissionId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted])
                SELECT NEWID(), [role].[Id], @permissionId, GETDATE(), 'migration', GETDATE(), 'migration', 0
                FROM [SystemRoles] AS [role]
                WHERE [role].[Code] = 'admin'
                    AND [role].[IsDeleted] = 0
                    AND NOT EXISTS
                    (
                        SELECT 1
                        FROM [SystemRolePermissions] AS [rolePermission]
                        WHERE [rolePermission].[RoleId] = [role].[Id]
                            AND [rolePermission].[PermissionId] = @permissionId
                            AND [rolePermission].[IsDeleted] = 0
                    );
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE [rolePermission]
                FROM [SystemRolePermissions] AS [rolePermission]
                INNER JOIN [SystemPermissions] AS [permission]
                    ON [permission].[Id] = [rolePermission].[PermissionId]
                WHERE [permission].[Code] = 'CRM_FOLLOW_TASK';

                DELETE FROM [SystemPermissions] WHERE [Code] = 'CRM_FOLLOW_TASK';
                """);

            migrationBuilder.DropIndex(
                name: "IX_CrmVendors_OwnerUserId_NextFollowAt",
                table: "CrmVendors");

            migrationBuilder.DropColumn(
                name: "LastFollowAt",
                table: "CrmVendors");

            migrationBuilder.DropColumn(
                name: "LastFollowResult",
                table: "CrmVendors");

            migrationBuilder.DropColumn(
                name: "NextFollowAt",
                table: "CrmVendors");

        }
    }
}
