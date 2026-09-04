using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmHerbBaseSupplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrmHerbBaseSupplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbBaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbBaseSubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuantityUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Specification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QualityRequirement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HarvestSeason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpectedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PriceUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SupplyCycle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmHerbBaseSupplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmHerbBaseSupplies_CrmHerbBases_HerbBaseId",
                        column: x => x.HerbBaseId,
                        principalTable: "CrmHerbBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrmHerbBaseSupplies_HerbBaseId_Status",
                table: "CrmHerbBaseSupplies",
                columns: new[] { "HerbBaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmHerbBaseSupplies_HerbBaseSubjectId_Status",
                table: "CrmHerbBaseSupplies",
                columns: new[] { "HerbBaseSubjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmHerbBaseSupplies_ProductName_Status_ValidUntil",
                table: "CrmHerbBaseSupplies",
                columns: new[] { "ProductName", "Status", "ValidUntil" });

            migrationBuilder.Sql(@"
                INSERT INTO dbo.CrmHerbBaseSupplies
                    (Id, HerbBaseId, HerbBaseSubjectId, ProductName, AvailableQuantity, QuantityUnit, Specification, QualityRequirement, HarvestSeason, ExpectedPrice, PriceUnit, SupplyCycle, ConfirmedAt, ValidUntil, Status, Remark, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
                SELECT NEWID(), base.Id, base.HerbBaseSubjectId, attribute.AttributeValue, NULL, N'', N'', N'', N'', NULL, N'', N'', NULL, NULL, N'待确认', N'', GETDATE(), 'migration', GETDATE(), 'migration', 0
                FROM dbo.CrmHerbBases base
                INNER JOIN dbo.CrmBusinessEntityAttributes attribute ON attribute.EntityId = base.Id
                WHERE base.IsDeleted = 0 AND attribute.IsDeleted = 0
                  AND attribute.EntityType = 'CRM_HERB_BASE' AND attribute.AttributeCode = 'MAIN_PRODUCT'
                  AND LTRIM(RTRIM(attribute.AttributeValue)) <> ''
                  AND NOT EXISTS (
                    SELECT 1 FROM dbo.CrmHerbBaseSupplies supply
                    WHERE supply.HerbBaseId = base.Id AND supply.ProductName = attribute.AttributeValue AND supply.IsDeleted = 0);

                DECLARE @rootId uniqueidentifier = (SELECT TOP (1) Id FROM dbo.SystemPermissions WHERE Code = 'ROOT' AND IsDeleted = 0);
                DECLARE @viewId uniqueidentifier = (SELECT TOP (1) Id FROM dbo.SystemPermissions WHERE Code = 'CRM_HERB_BASE_SUPPLY_VIEW' AND IsDeleted = 0);
                IF @viewId IS NULL BEGIN
                    SET @viewId = NEWID();
                    INSERT INTO dbo.SystemPermissions (Id, Name, Code, ParentId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
                    VALUES (@viewId, N'查看供应信息', 'CRM_HERB_BASE_SUPPLY_VIEW', @rootId, GETDATE(), 'migration', GETDATE(), 'migration', 0);
                END;
                DECLARE @manageId uniqueidentifier = (SELECT TOP (1) Id FROM dbo.SystemPermissions WHERE Code = 'CRM_HERB_BASE_SUPPLY_MANAGE' AND IsDeleted = 0);
                IF @manageId IS NULL BEGIN
                    SET @manageId = NEWID();
                    INSERT INTO dbo.SystemPermissions (Id, Name, Code, ParentId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
                    VALUES (@manageId, N'维护供应信息', 'CRM_HERB_BASE_SUPPLY_MANAGE', @rootId, GETDATE(), 'migration', GETDATE(), 'migration', 0);
                END;
                INSERT INTO dbo.SystemRolePermissions (Id, RoleId, PermissionId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
                SELECT NEWID(), role.Id, permission.Id, GETDATE(), 'migration', GETDATE(), 'migration', 0
                FROM dbo.SystemRoles role CROSS JOIN dbo.SystemPermissions permission
                WHERE role.Code = 'admin' AND role.IsDeleted = 0 AND permission.Code IN ('CRM_HERB_BASE_SUPPLY_VIEW', 'CRM_HERB_BASE_SUPPLY_MANAGE') AND permission.IsDeleted = 0
                  AND NOT EXISTS (SELECT 1 FROM dbo.SystemRolePermissions assigned WHERE assigned.RoleId = role.Id AND assigned.PermissionId = permission.Id AND assigned.IsDeleted = 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE assigned FROM dbo.SystemRolePermissions assigned
                INNER JOIN dbo.SystemPermissions permission ON permission.Id = assigned.PermissionId
                WHERE permission.Code IN ('CRM_HERB_BASE_SUPPLY_VIEW', 'CRM_HERB_BASE_SUPPLY_MANAGE');
                DELETE FROM dbo.SystemPermissions WHERE Code IN ('CRM_HERB_BASE_SUPPLY_VIEW', 'CRM_HERB_BASE_SUPPLY_MANAGE');
            ");
            migrationBuilder.DropTable(
                name: "CrmHerbBaseSupplies");
        }
    }
}
