using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemovePurchaseDemandProductAttributeProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM dbo.CrmBusinessEntityAttributes WHERE EntityType = 'CRM_PURCHASE_DEMAND' AND AttributeCode = 'PURCHASE_PRODUCT';");

            migrationBuilder.DropIndex(
                name: "IX_CrmBusinessEntityAttributes_PurchaseDemand_Product",
                table: "CrmBusinessEntityAttributes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CrmBusinessEntityAttributes_PurchaseDemand_Product",
                table: "CrmBusinessEntityAttributes",
                columns: new[] { "EntityId", "AttributeValue" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [EntityType] = 'CRM_PURCHASE_DEMAND' AND [AttributeCode] = 'PURCHASE_PRODUCT'");
        }
    }
}
