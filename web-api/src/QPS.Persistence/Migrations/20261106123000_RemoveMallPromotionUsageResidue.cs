using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMallPromotionUsageResidue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MallPromotionUsages_PromotionId_UserId",
                table: "MallPromotionUsages");

            migrationBuilder.DropTable(
                name: "MallPromotionUsages");

            migrationBuilder.DropColumn(
                name: "MaxUsagePerOrder",
                table: "MallPromotions");

            migrationBuilder.DropColumn(
                name: "MaxUsagePerUser",
                table: "MallPromotions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxUsagePerUser",
                table: "MallPromotions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsagePerOrder",
                table: "MallPromotions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MallPromotionUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallPromotionUsages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MallPromotionUsages_PromotionId_UserId",
                table: "MallPromotionUsages",
                columns: new[] { "PromotionId", "UserId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
