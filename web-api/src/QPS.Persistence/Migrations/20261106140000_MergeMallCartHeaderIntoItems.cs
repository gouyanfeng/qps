using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <summary>
    /// 去掉购物车车头 MallCarts：明细行直接带 UserId，唯一索引改为 (UserId, SkuId)；
    /// 父车已软删或已不存在的孤儿明细直接清除，其余明细从车头回填 UserId。
    /// </summary>
    public partial class MergeMallCartHeaderIntoItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "MallCartItems",
                type: "uniqueidentifier",
                nullable: true);

            // 父车软删或不存在 => 孤儿明细直接清除
            migrationBuilder.Sql(
                "DELETE i FROM MallCartItems AS i " +
                "WHERE NOT EXISTS (SELECT 1 FROM MallCarts AS c WHERE c.Id = i.CartId AND c.IsDeleted = 0);");

            // 其余明细从仍有效的车头回填归属会员
            migrationBuilder.Sql(
                "UPDATE i SET i.UserId = c.UserId FROM MallCartItems AS i " +
                "INNER JOIN MallCarts AS c ON c.Id = i.CartId;");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "MallCartItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.DropIndex(
                name: "IX_MallCartItems_CartId_SkuId",
                table: "MallCartItems");

            migrationBuilder.DropColumn(
                name: "CartId",
                table: "MallCartItems");

            migrationBuilder.CreateIndex(
                name: "IX_MallCartItems_UserId_SkuId",
                table: "MallCartItems",
                columns: new[] { "UserId", "SkuId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.DropTable(
                name: "MallCarts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MallCarts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallCarts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MallCarts_UserId",
                table: "MallCarts",
                column: "UserId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.DropIndex(
                name: "IX_MallCartItems_UserId_SkuId",
                table: "MallCartItems");

            migrationBuilder.AddColumn<Guid>(
                name: "CartId",
                table: "MallCartItems",
                type: "uniqueidentifier",
                nullable: true);

            // 按会员重建车头并回填明细
            migrationBuilder.Sql(
                "INSERT INTO MallCarts (Id, UserId, LastActiveAt, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted) " +
                "SELECT NEWID(), i.UserId, SYSUTCDATETIME(), SYSUTCDATETIME(), '', SYSUTCDATETIME(), '', 0 " +
                "FROM (SELECT DISTINCT UserId FROM MallCartItems WHERE IsDeleted = 0) AS i;");

            migrationBuilder.Sql(
                "UPDATE i SET i.CartId = c.Id FROM MallCartItems AS i " +
                "INNER JOIN MallCarts AS c ON c.UserId = i.UserId;");

            migrationBuilder.AlterColumn<Guid>(
                name: "CartId",
                table: "MallCartItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "MallCartItems");

            migrationBuilder.CreateIndex(
                name: "IX_MallCartItems_CartId_SkuId",
                table: "MallCartItems",
                columns: new[] { "CartId", "SkuId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
