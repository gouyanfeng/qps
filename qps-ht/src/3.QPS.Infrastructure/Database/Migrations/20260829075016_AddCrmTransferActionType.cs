using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmTransferActionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                table: "CrmTransferRecords",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "ENTRY");

            migrationBuilder.Sql("""
                WITH OrderedRecords AS
                (
                    SELECT Id,
                        ROW_NUMBER() OVER (PARTITION BY EntityType, EntityId ORDER BY CreatedAt, Id) AS Sequence,
                        FromOwnerUserId,
                        ToOwnerUserId
                    FROM CrmTransferRecords
                    WHERE IsDeleted = 0
                )
                UPDATE record
                SET ActionType = CASE
                    WHEN ordered.Sequence = 1 THEN 'ENTRY'
                    WHEN ordered.FromOwnerUserId IS NULL AND ordered.ToOwnerUserId IS NOT NULL THEN 'ASSIGN'
                    WHEN ordered.FromOwnerUserId IS NOT NULL AND ordered.ToOwnerUserId IS NULL THEN 'RETURN'
                    WHEN ordered.FromOwnerUserId IS NOT NULL AND ordered.ToOwnerUserId IS NOT NULL THEN 'TRANSFER'
                    ELSE 'ENTRY'
                END
                FROM CrmTransferRecords record
                INNER JOIN OrderedRecords ordered ON ordered.Id = record.Id;
                """);

            migrationBuilder.Sql("""
                INSERT INTO CrmTransferRecords
                    (Id, ActionType, EntityType, EntityId, FromOwnerUserId, ToOwnerUserId, OperatorUserId, Remark, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
                SELECT NEWID(), 'ENTRY', 'CRM_HERB_BASE_SUBJECT', subject.Id, NULL, subject.OwnerUserId, NULL, N'历史数据补录',
                    subject.CreatedAt, subject.CreatedBy, subject.UpdatedAt, subject.UpdatedBy, 0
                FROM CrmHerbBaseSubjects subject
                WHERE subject.IsDeleted = 0
                    AND NOT EXISTS
                    (
                        SELECT 1
                        FROM CrmTransferRecords record
                        WHERE record.EntityType = 'CRM_HERB_BASE_SUBJECT'
                            AND record.EntityId = subject.Id
                            AND record.IsDeleted = 0
                    );
                """);

            migrationBuilder.Sql("""
                DECLARE @permissionId uniqueidentifier =
                    (SELECT TOP (1) [Id] FROM [SystemPermissions] WHERE [Code] = 'CRM_TRANSFER' AND [IsDeleted] = 0);

                IF @permissionId IS NULL
                BEGIN
                    DECLARE @rootId uniqueidentifier =
                        (SELECT TOP (1) [Id] FROM [SystemPermissions] WHERE [Code] = 'ROOT' AND [IsDeleted] = 0);

                    IF @rootId IS NULL
                        THROW 50000, '未找到 ROOT 权限，无法创建流转权限。', 1;

                    SET @permissionId = NEWID();

                    INSERT INTO [SystemPermissions]
                        ([Id], [Name], [Code], [ParentId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted])
                    VALUES
                        (@permissionId, N'CRM流转', 'CRM_TRANSFER', @rootId, GETDATE(), 'migration', GETDATE(), 'migration', 0);
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

            migrationBuilder.Sql("""
                INSERT INTO CrmTransferRecords
                    (Id, ActionType, EntityType, EntityId, FromOwnerUserId, ToOwnerUserId, OperatorUserId, Remark, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
                SELECT NEWID(), 'ENTRY', 'CRM_VENDOR', vendor.Id, NULL, vendor.OwnerUserId, NULL, N'历史数据补录',
                    vendor.CreatedAt, vendor.CreatedBy, vendor.UpdatedAt, vendor.UpdatedBy, 0
                FROM CrmVendors vendor
                WHERE vendor.IsDeleted = 0
                    AND NOT EXISTS
                    (
                        SELECT 1
                        FROM CrmTransferRecords record
                        WHERE record.EntityType = 'CRM_VENDOR'
                            AND record.EntityId = vendor.Id
                            AND record.IsDeleted = 0
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
                WHERE [permission].[Code] = 'CRM_TRANSFER';

                DELETE FROM [SystemPermissions] WHERE [Code] = 'CRM_TRANSFER';
                """);

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "CrmTransferRecords");
        }
    }
}
