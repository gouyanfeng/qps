using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.src.QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropDemandStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrmVendorDemands_VendorId_Status_DemandAt",
                table: "CrmVendorDemands");

            migrationBuilder.DropColumn(
                name: "ClosedReason",
                table: "CrmVendorDemands");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CrmVendorDemands");

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendorDemands_VendorId_DemandAt",
                table: "CrmVendorDemands",
                columns: new[] { "VendorId", "DemandAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrmVendorDemands_VendorId_DemandAt",
                table: "CrmVendorDemands");

            migrationBuilder.AddColumn<string>(
                name: "ClosedReason",
                table: "CrmVendorDemands",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CrmVendorDemands",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendorDemands_VendorId_Status_DemandAt",
                table: "CrmVendorDemands",
                columns: new[] { "VendorId", "Status", "DemandAt" });
        }
    }
}
