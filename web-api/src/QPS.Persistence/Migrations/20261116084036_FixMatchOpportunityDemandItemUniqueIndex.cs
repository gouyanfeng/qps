using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixMatchOpportunityDemandItemUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrmMatchOpportunities_DemandItemId",
                table: "CrmMatchOpportunities");

            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchOpportunities_DemandItemId",
                table: "CrmMatchOpportunities",
                column: "DemandItemId",
                unique: true,
                filter: "[DemandItemId] IS NOT NULL AND [Status] IN (N'推进中', N'暂停') AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrmMatchOpportunities_DemandItemId",
                table: "CrmMatchOpportunities");

            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchOpportunities_DemandItemId",
                table: "CrmMatchOpportunities",
                column: "DemandItemId",
                unique: true,
                filter: "[Status] IN (N'推进中', N'暂停') AND [IsDeleted] = 0");
        }
    }
}
