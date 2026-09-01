using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations;

public partial class MigrateVendorPurchasePlansToPurchaseDemands : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_CrmBusinessEntityAttributes_VendorPurchasePlan_Product", table: "CrmBusinessEntityAttributes");
        migrationBuilder.DropIndex(name: "IX_CrmVendorPurchasePlans_PageUrl", table: "CrmVendorPurchasePlans");
        migrationBuilder.DropIndex(name: "IX_CrmVendorPurchasePlans_PurchaseTime", table: "CrmVendorPurchasePlans");
        migrationBuilder.DropIndex(name: "IX_CrmVendorPurchasePlans_VendorId", table: "CrmVendorPurchasePlans");
        migrationBuilder.RenameTable(name: "CrmVendorPurchasePlans", newName: "CrmPurchaseDemands");
        migrationBuilder.RenameColumn(name: "PurchasePlanName", table: "CrmPurchaseDemands", newName: "DemandName");
        migrationBuilder.RenameColumn(name: "PurchaseTime", table: "CrmPurchaseDemands", newName: "DemandAt");
        migrationBuilder.RenameColumn(name: "PageUrl", table: "CrmPurchaseDemands", newName: "SourceUrl");
        migrationBuilder.RenameColumn(name: "LatestPurchasePlanName", table: "CrmVendors", newName: "LatestPurchaseDemandName");
        migrationBuilder.Sql("UPDATE dbo.CrmPurchaseDemands SET DemandAt = COALESCE(DemandAt, CreatedAt);");
        migrationBuilder.AlterColumn<DateTime>(name: "DemandAt", table: "CrmPurchaseDemands", nullable: false, oldClrType: typeof(DateTime), oldType: "datetime2", oldNullable: true);
        migrationBuilder.AddColumn<Guid>(name: "ContactId", table: "CrmPurchaseDemands", nullable: true);
        migrationBuilder.AddColumn<string>(name: "DemandNo", table: "CrmPurchaseDemands", maxLength: 64, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "Status", table: "CrmPurchaseDemands", maxLength: 32, nullable: false, defaultValue: "待确认");
        migrationBuilder.AddColumn<string>(name: "SourceType", table: "CrmPurchaseDemands", maxLength: 32, nullable: false, defaultValue: "外部来源");
        migrationBuilder.AddColumn<DateTime>(name: "ExpectedDeliveryAt", table: "CrmPurchaseDemands", nullable: true);
        migrationBuilder.AddColumn<string>(name: "ReceivingAddress", table: "CrmPurchaseDemands", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "ClosedReason", table: "CrmPurchaseDemands", nullable: false, defaultValue: "");
        migrationBuilder.Sql("UPDATE dbo.CrmPurchaseDemands SET DemandNo = CONCAT('PD', CONVERT(char(8), CreatedAt, 112), RIGHT(REPLACE(CONVERT(varchar(36), Id), '-', ''), 12)), SourceType = N'外部来源', Status = N'待确认', Remark = CASE WHEN NULLIF(LTRIM(RTRIM(Products)), N'') IS NULL THEN Remark ELSE CONCAT(Remark, CASE WHEN Remark = N'' THEN N'' ELSE N'；' END, N'历史品类摘要：', Products) END;");
        migrationBuilder.CreateTable(name: "CrmPurchaseDemandItems", columns: table => new { Id = table.Column<Guid>(nullable: false), PurchaseDemandId = table.Column<Guid>(nullable: false), ProductName = table.Column<string>(maxLength: 200, nullable: false), Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: true), QuantityUnit = table.Column<string>(nullable: false), Specification = table.Column<string>(nullable: false), QualityRequirement = table.Column<string>(nullable: false), TargetPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true), PriceUnit = table.Column<string>(nullable: false), ExpectedDeliveryAt = table.Column<DateTime>(nullable: true), Remark = table.Column<string>(nullable: false), SortOrder = table.Column<int>(nullable: false), CreatedAt = table.Column<DateTime>(nullable: false), CreatedBy = table.Column<string>(nullable: false), UpdatedAt = table.Column<DateTime>(nullable: false), UpdatedBy = table.Column<string>(nullable: false), IsDeleted = table.Column<bool>(nullable: false) }, constraints: table => { table.PrimaryKey("PK_CrmPurchaseDemandItems", x => x.Id); table.ForeignKey("FK_CrmPurchaseDemandItems_CrmPurchaseDemands_PurchaseDemandId", x => x.PurchaseDemandId, "CrmPurchaseDemands", "Id", onDelete: ReferentialAction.Cascade); });
        migrationBuilder.Sql("INSERT INTO dbo.CrmPurchaseDemandItems (Id, PurchaseDemandId, ProductName, QuantityUnit, Specification, QualityRequirement, PriceUnit, Remark, SortOrder, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted) SELECT NEWID(), a.EntityId, a.AttributeValue, N'', N'', N'', N'', N'', a.SortOrder, a.CreatedAt, a.CreatedBy, a.UpdatedAt, a.UpdatedBy, a.IsDeleted FROM dbo.CrmBusinessEntityAttributes a WHERE a.EntityType = 'CRM_VENDOR_PURCHASE_PLAN' AND a.AttributeCode = 'PURCHASE_PRODUCT'; UPDATE dbo.CrmBusinessEntityAttributes SET EntityType = 'CRM_PURCHASE_DEMAND' WHERE EntityType = 'CRM_VENDOR_PURCHASE_PLAN' AND AttributeCode = 'PURCHASE_PRODUCT'; UPDATE dbo.SystemOperationLogs SET EntityType = 'CrmPurchaseDemand' WHERE EntityType = 'CrmVendorPurchasePlan';");
        migrationBuilder.DropColumn(name: "Products", table: "CrmPurchaseDemands");
        migrationBuilder.CreateIndex(name: "IX_CrmBusinessEntityAttributes_PurchaseDemand_Product", table: "CrmBusinessEntityAttributes", columns: new[] { "EntityId", "AttributeValue" }, unique: true, filter: "[IsDeleted] = 0 AND [EntityType] = 'CRM_PURCHASE_DEMAND' AND [AttributeCode] = 'PURCHASE_PRODUCT'");
        migrationBuilder.CreateIndex(name: "IX_CrmPurchaseDemands_DemandNo", table: "CrmPurchaseDemands", column: "DemandNo", unique: true);
        migrationBuilder.CreateIndex(name: "IX_CrmPurchaseDemands_VendorId_Status_DemandAt", table: "CrmPurchaseDemands", columns: new[] { "VendorId", "Status", "DemandAt" });
        migrationBuilder.CreateIndex(name: "IX_CrmPurchaseDemands_ContactId", table: "CrmPurchaseDemands", column: "ContactId");
        migrationBuilder.CreateIndex(name: "IX_CrmPurchaseDemandItems_PurchaseDemandId_SortOrder", table: "CrmPurchaseDemandItems", columns: new[] { "PurchaseDemandId", "SortOrder" });
        migrationBuilder.AddForeignKey(name: "FK_CrmPurchaseDemands_CrmContacts_ContactId", table: "CrmPurchaseDemands", column: "ContactId", principalTable: "CrmContacts", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.Sql("""
            DECLARE @rootId uniqueidentifier = (SELECT TOP (1) Id FROM SystemPermissions WHERE Code = 'ROOT' AND IsDeleted = 0);
            IF @rootId IS NULL THROW 50000, '未找到 ROOT 权限。', 1;
            DECLARE @viewId uniqueidentifier = (SELECT TOP (1) Id FROM SystemPermissions WHERE Code = 'CRM_PURCHASE_DEMAND_VIEW' AND IsDeleted = 0);
            IF @viewId IS NULL BEGIN SET @viewId = NEWID(); INSERT INTO SystemPermissions (Id,Name,Code,ParentId,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy,IsDeleted) VALUES (@viewId,N'查看采购需求','CRM_PURCHASE_DEMAND_VIEW',@rootId,GETDATE(),'migration',GETDATE(),'migration',0); END;
            DECLARE @manageId uniqueidentifier = (SELECT TOP (1) Id FROM SystemPermissions WHERE Code = 'CRM_PURCHASE_DEMAND_MANAGE' AND IsDeleted = 0);
            IF @manageId IS NULL BEGIN SET @manageId = NEWID(); INSERT INTO SystemPermissions (Id,Name,Code,ParentId,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy,IsDeleted) VALUES (@manageId,N'维护采购需求','CRM_PURCHASE_DEMAND_MANAGE',@rootId,GETDATE(),'migration',GETDATE(),'migration',0); END;
            INSERT INTO SystemRolePermissions (Id,RoleId,PermissionId,CreatedAt,CreatedBy,UpdatedAt,UpdatedBy,IsDeleted)
            SELECT NEWID(), r.Id, p.Id, GETDATE(),'migration',GETDATE(),'migration',0 FROM SystemRoles r CROSS JOIN (SELECT @viewId Id UNION ALL SELECT @manageId) p
            WHERE r.Code = 'admin' AND r.IsDeleted = 0 AND NOT EXISTS (SELECT 1 FROM SystemRolePermissions rp WHERE rp.RoleId=r.Id AND rp.PermissionId=p.Id AND rp.IsDeleted=0);
            """);
    }
    protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("采购需求迁移不可自动回滚，请从备份恢复。");
}
