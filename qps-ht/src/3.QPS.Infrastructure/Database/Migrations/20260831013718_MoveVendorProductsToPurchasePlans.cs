using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class MoveVendorProductsToPurchasePlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CrmBusinessEntityAttributes_VendorPurchasePlan_Product",
                table: "CrmBusinessEntityAttributes",
                columns: new[] { "EntityId", "AttributeValue" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [EntityType] = 'CRM_VENDOR_PURCHASE_PLAN' AND [AttributeCode] = 'PURCHASE_PRODUCT'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrmBusinessEntityAttributes_VendorPurchasePlan_Product",
                table: "CrmBusinessEntityAttributes");
        }
    }
}
