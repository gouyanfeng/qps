using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class BackfillCrmVendorPurchasePlanSummaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrmVendorPurchasePlans_PageUrl",
                table: "CrmVendorPurchasePlans");

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendorPurchasePlans_PageUrl",
                table: "CrmVendorPurchasePlans",
                column: "PageUrl",
                unique: true,
                filter: "[PageUrl] <> ''");

            migrationBuilder.Sql("""
                INSERT INTO CrmVendorPurchasePlans
                    (Id, VendorId, PurchasePlanName, PurchaseTime, Products, PageUrl, Remark, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
                SELECT NEWID(), vendor.Id,
                    COALESCE(NULLIF(LTRIM(RTRIM(vendor.LatestPurchasePlanName)), N''), N'历史采购计划'),
                    vendor.LatestPurchaseTime, N'', N'', N'历史采购摘要回填',
                    vendor.CreatedAt, 'migration', vendor.UpdatedAt, 'migration', 0
                FROM CrmVendors vendor
                WHERE vendor.IsDeleted = 0
                    AND (NULLIF(LTRIM(RTRIM(vendor.LatestPurchasePlanName)), N'') IS NOT NULL OR vendor.LatestPurchaseTime IS NOT NULL)
                    AND NOT EXISTS
                    (
                        SELECT 1
                        FROM CrmVendorPurchasePlans purchasePlan
                        WHERE purchasePlan.VendorId = vendor.Id
                            AND purchasePlan.IsDeleted = 0
                    );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM CrmVendorPurchasePlans
                WHERE CreatedBy = 'migration'
                    AND UpdatedBy = 'migration'
                    AND Remark = N'历史采购摘要回填';
                """);

            migrationBuilder.DropIndex(
                name: "IX_CrmVendorPurchasePlans_PageUrl",
                table: "CrmVendorPurchasePlans");

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendorPurchasePlans_PageUrl",
                table: "CrmVendorPurchasePlans",
                column: "PageUrl",
                unique: true);
        }
    }
}
