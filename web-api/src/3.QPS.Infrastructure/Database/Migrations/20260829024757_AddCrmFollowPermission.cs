using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmFollowPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DECLARE @permissionId uniqueidentifier =
                    (SELECT TOP (1) [Id] FROM [SystemPermissions] WHERE [Code] = 'CRM_FOLLOW' AND [IsDeleted] = 0);

                IF @permissionId IS NULL
                BEGIN
                    DECLARE @rootId uniqueidentifier =
                        (SELECT TOP (1) [Id] FROM [SystemPermissions] WHERE [Code] = 'ROOT' AND [IsDeleted] = 0);

                    IF @rootId IS NULL
                        THROW 50000, '未找到 ROOT 权限，无法创建记录跟进权限。', 1;

                    SET @permissionId = NEWID();

                    INSERT INTO [SystemPermissions]
                        ([Id], [Name], [Code], [ParentId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted])
                    VALUES
                        (@permissionId, N'记录跟进', 'CRM_FOLLOW', @rootId, GETDATE(), 'migration', GETDATE(), 'migration', 0);
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
                WHERE [permission].[Code] = 'CRM_FOLLOW';

                DELETE FROM [SystemPermissions] WHERE [Code] = 'CRM_FOLLOW';
                """);
        }
    }
}
