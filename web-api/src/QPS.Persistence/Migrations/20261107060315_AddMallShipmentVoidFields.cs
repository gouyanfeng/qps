using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMallShipmentVoidFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MallShipments_OrderId",
                table: "MallShipments");

            migrationBuilder.DropIndex(
                name: "IX_MallShipments_TrackingNo",
                table: "MallShipments");

            migrationBuilder.AddColumn<bool>(
                name: "IsVoided",
                table: "MallShipments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "MallShipments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "VoidedAt",
                table: "MallShipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidedBy",
                table: "MallShipments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MallShipments_OrderId",
                table: "MallShipments",
                column: "OrderId",
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsVoided] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallShipments_TrackingNo",
                table: "MallShipments",
                column: "TrackingNo",
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsVoided] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MallShipments_OrderId",
                table: "MallShipments");

            migrationBuilder.DropIndex(
                name: "IX_MallShipments_TrackingNo",
                table: "MallShipments");

            migrationBuilder.DropColumn(
                name: "IsVoided",
                table: "MallShipments");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "MallShipments");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "MallShipments");

            migrationBuilder.DropColumn(
                name: "VoidedBy",
                table: "MallShipments");

            migrationBuilder.CreateIndex(
                name: "IX_MallShipments_OrderId",
                table: "MallShipments",
                column: "OrderId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallShipments_TrackingNo",
                table: "MallShipments",
                column: "TrackingNo",
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
