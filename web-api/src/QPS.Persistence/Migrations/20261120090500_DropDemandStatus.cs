using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <summary>
    /// 删除采购单单据级 Status 和 ClosedReason 字段，该功能已由明细级 MatchStatus（空闲/撮合中/成交）完全替代。
    /// </summary>
    public partial class DropDemandStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrmVendorDemands_VendorId_Status_DemandAt",
                table: "CrmVendorDemands");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CrmVendorDemands");

            migrationBuilder.DropColumn(
                name: "ClosedReason",
                table: "CrmVendorDemands");

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendorDemands_VendorId_DemandAt",
                table: "CrmVendorDemands",
                columns: new[] { "VendorId", "DemandAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrmVendorDemands_VendorId_DemandAt",
                table: "CrmVendorDemands");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CrmVendorDemands",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "待确认");

            migrationBuilder.AddColumn<string>(
                name: "ClosedReason",
                table: "CrmVendorDemands",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendorDemands_VendorId_Status_DemandAt",
                table: "CrmVendorDemands",
                columns: new[] { "VendorId", "Status", "DemandAt" });
        }
    }
}
