using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMallContentEnhancement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WarnStock",
                table: "MallSkus",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AlbumImages",
                table: "MallProducts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "MallProducts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetailHtml",
                table: "MallProducts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MainImageUrl",
                table: "MallProducts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "MallProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MallCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallStockLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkuCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SpecName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ChangeQuantity = table.Column<int>(type: "int", nullable: false),
                    BeforeStock = table.Column<int>(type: "int", nullable: false),
                    AfterStock = table.Column<int>(type: "int", nullable: false),
                    RelatedOrderNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallStockLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MallProducts_CategoryId",
                table: "MallProducts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MallCategories_Code",
                table: "MallCategories",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallStockLogs_ChangeType_CreatedAt",
                table: "MallStockLogs",
                columns: new[] { "ChangeType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MallStockLogs_SkuId",
                table: "MallStockLogs",
                column: "SkuId");

            migrationBuilder.AddForeignKey(
                name: "FK_MallProducts_MallCategories_CategoryId",
                table: "MallProducts",
                column: "CategoryId",
                principalTable: "MallCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MallProducts_MallCategories_CategoryId",
                table: "MallProducts");

            migrationBuilder.DropTable(
                name: "MallCategories");

            migrationBuilder.DropTable(
                name: "MallStockLogs");

            migrationBuilder.DropIndex(
                name: "IX_MallProducts_CategoryId",
                table: "MallProducts");

            migrationBuilder.DropColumn(
                name: "WarnStock",
                table: "MallSkus");

            migrationBuilder.DropColumn(
                name: "AlbumImages",
                table: "MallProducts");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "MallProducts");

            migrationBuilder.DropColumn(
                name: "DetailHtml",
                table: "MallProducts");

            migrationBuilder.DropColumn(
                name: "MainImageUrl",
                table: "MallProducts");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "MallProducts");
        }
    }
}
