using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameCrmPurchaseDemandsToVendorDemands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrmPurchaseDemandItems_CrmPurchaseDemands_PurchaseDemandId",
                table: "CrmPurchaseDemandItems");

            migrationBuilder.RenameTable(name: "CrmPurchaseDemands", newName: "CrmVendorDemands");
            migrationBuilder.RenameTable(name: "CrmPurchaseDemandItems", newName: "CrmVendorDemandItems");
            migrationBuilder.RenameColumn(name: "PurchaseDemandId", table: "CrmVendorDemandItems", newName: "VendorDemandId");

            migrationBuilder.RenameIndex(
                name: "IX_CrmPurchaseDemandItems_PurchaseDemandId_SortOrder",
                table: "CrmVendorDemandItems",
                newName: "IX_CrmVendorDemandItems_VendorDemandId_SortOrder");
            migrationBuilder.RenameIndex(name: "IX_CrmPurchaseDemands_ContactId", table: "CrmVendorDemands", newName: "IX_CrmVendorDemands_ContactId");
            migrationBuilder.RenameIndex(name: "IX_CrmPurchaseDemands_DemandNo", table: "CrmVendorDemands", newName: "IX_CrmVendorDemands_DemandNo");
            migrationBuilder.RenameIndex(name: "IX_CrmPurchaseDemands_VendorId_Status_DemandAt", table: "CrmVendorDemands", newName: "IX_CrmVendorDemands_VendorId_Status_DemandAt");

            migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[PK_CrmVendorPurchasePlans]', N'PK') IS NOT NULL EXEC sp_rename N'[dbo].[PK_CrmVendorPurchasePlans]', N'PK_CrmVendorDemands', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[PK_CrmPurchaseDemandItems]', N'PK_CrmVendorDemandItems', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[FK_CrmPurchaseDemands_CrmContacts_ContactId]', N'FK_CrmVendorDemands_CrmContacts_ContactId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[FK_CrmVendorPurchasePlans_CrmVendors_VendorId]', N'FK_CrmVendorDemands_CrmVendors_VendorId', N'OBJECT';");

            migrationBuilder.AddForeignKey(
                name: "FK_CrmVendorDemandItems_CrmVendorDemands_VendorDemandId",
                table: "CrmVendorDemandItems",
                column: "VendorDemandId",
                principalTable: "CrmVendorDemands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CrmVendorDemandItems_CrmVendorDemands_VendorDemandId",
                table: "CrmVendorDemandItems");

            migrationBuilder.RenameIndex(
                name: "IX_CrmVendorDemandItems_VendorDemandId_SortOrder",
                table: "CrmVendorDemandItems",
                newName: "IX_CrmPurchaseDemandItems_PurchaseDemandId_SortOrder");
            migrationBuilder.RenameIndex(name: "IX_CrmVendorDemands_ContactId", table: "CrmVendorDemands", newName: "IX_CrmPurchaseDemands_ContactId");
            migrationBuilder.RenameIndex(name: "IX_CrmVendorDemands_DemandNo", table: "CrmVendorDemands", newName: "IX_CrmPurchaseDemands_DemandNo");
            migrationBuilder.RenameIndex(name: "IX_CrmVendorDemands_VendorId_Status_DemandAt", table: "CrmVendorDemands", newName: "IX_CrmPurchaseDemands_VendorId_Status_DemandAt");

            migrationBuilder.Sql("IF OBJECT_ID(N'[dbo].[PK_CrmVendorDemands]', N'PK') IS NOT NULL EXEC sp_rename N'[dbo].[PK_CrmVendorDemands]', N'PK_CrmVendorPurchasePlans', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[PK_CrmVendorDemandItems]', N'PK_CrmPurchaseDemandItems', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[FK_CrmVendorDemands_CrmContacts_ContactId]', N'FK_CrmPurchaseDemands_CrmContacts_ContactId', N'OBJECT';");
            migrationBuilder.Sql("EXEC sp_rename N'[dbo].[FK_CrmVendorDemands_CrmVendors_VendorId]', N'FK_CrmVendorPurchasePlans_CrmVendors_VendorId', N'OBJECT';");

            migrationBuilder.RenameColumn(name: "VendorDemandId", table: "CrmVendorDemandItems", newName: "PurchaseDemandId");
            migrationBuilder.RenameTable(name: "CrmVendorDemandItems", newName: "CrmPurchaseDemandItems");
            migrationBuilder.RenameTable(name: "CrmVendorDemands", newName: "CrmPurchaseDemands");

            migrationBuilder.AddForeignKey(
                name: "FK_CrmPurchaseDemandItems_CrmPurchaseDemands_PurchaseDemandId",
                table: "CrmPurchaseDemandItems",
                column: "PurchaseDemandId",
                principalTable: "CrmPurchaseDemands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
