using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.src.QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMallDashboardPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 商城数据看板权限：父节点按 Code 查，不硬编码 GUID；幂等（不存在才插入）。
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM [dbo].[SystemPermissions] WHERE [Code] = N'MALL_DASHBOARD_VIEW')
BEGIN
    DECLARE @parentId uniqueidentifier = (SELECT TOP 1 [Id] FROM [dbo].[SystemPermissions] WHERE [Code] = N'MALL');
    INSERT INTO [dbo].[SystemPermissions]
        ([Id], [Name], [Code], [ParentId], [PermissionType], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted])
    VALUES
        (NEWID(), N'数据看板', N'MALL_DASHBOARD_VIEW', @parentId, N'Menu', GETDATE(), N'migration', GETDATE(), N'migration', 0);
END

-- admin 角色授权（角色按 Code 查），NOT EXISTS 避免重复
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[SystemRolePermissions] rp
    JOIN [dbo].[SystemPermissions] p ON rp.[PermissionId] = p.[Id]
    JOIN [dbo].[SystemRoles] r ON rp.[RoleId] = r.[Id]
    WHERE p.[Code] = N'MALL_DASHBOARD_VIEW' AND r.[Code] = N'admin'
)
BEGIN
    DECLARE @adminRoleId uniqueidentifier = (SELECT TOP 1 [Id] FROM [dbo].[SystemRoles] WHERE [Code] = N'admin');
    DECLARE @permissionId uniqueidentifier = (SELECT TOP 1 [Id] FROM [dbo].[SystemPermissions] WHERE [Code] = N'MALL_DASHBOARD_VIEW');
    INSERT INTO [dbo].[SystemRolePermissions]
        ([Id], [RoleId], [PermissionId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted])
    VALUES
        (NEWID(), @adminRoleId, @permissionId, GETDATE(), N'migration', GETDATE(), N'migration', 0);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE rp FROM [dbo].[SystemRolePermissions] rp
JOIN [dbo].[SystemPermissions] p ON rp.[PermissionId] = p.[Id]
WHERE p.[Code] = N'MALL_DASHBOARD_VIEW';

DELETE FROM [dbo].[SystemPermissions] WHERE [Code] = N'MALL_DASHBOARD_VIEW';
");
        }
    }
}
