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
            migrationBuilder.CreateTable(
                name: "__MoveVendorProductsToPurchasePlans",
                columns: table => new
                {
                    AttributeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK___MoveVendorProductsToPurchasePlans", column => column.AttributeId);
                });

            migrationBuilder.Sql("""
                INSERT INTO [__MoveVendorProductsToPurchasePlans] ([AttributeId])
                SELECT [Id]
                FROM [CrmBusinessEntityAttributes]
                WHERE [IsDeleted] = 0
                  AND [EntityType] = 'CRM_VENDOR'
                  AND [AttributeCode] = 'PURCHASE_PRODUCT';

                UPDATE [attribute]
                SET [IsDeleted] = 1,
                    [UpdatedAt] = GETDATE(),
                    [UpdatedBy] = 'migration'
                FROM [CrmBusinessEntityAttributes] AS [attribute]
                INNER JOIN [__MoveVendorProductsToPurchasePlans] AS [journal]
                    ON [journal].[AttributeId] = [attribute].[Id];
                """);

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
            migrationBuilder.Sql("""
                UPDATE [attribute]
                SET [IsDeleted] = 0,
                    [UpdatedAt] = GETDATE(),
                    [UpdatedBy] = 'migration'
                FROM [CrmBusinessEntityAttributes] AS [attribute]
                INNER JOIN [__MoveVendorProductsToPurchasePlans] AS [journal]
                    ON [journal].[AttributeId] = [attribute].[Id]
                WHERE [attribute].[IsDeleted] = 1
                  AND [attribute].[UpdatedBy] = 'migration';
                """);

            migrationBuilder.DropIndex(
                name: "IX_CrmBusinessEntityAttributes_VendorPurchasePlan_Product",
                table: "CrmBusinessEntityAttributes");

            migrationBuilder.DropTable(
                name: "__MoveVendorProductsToPurchasePlans");
        }
    }
}
