using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <summary>
    /// 购物车唯一索引补齐软删除过滤条件：历史迁移建唯一索引时漏掉了 [IsDeleted] = 0，
    /// 导致清空购物车（软删除购物车行）后再次加购触发唯一键冲突。
    /// </summary>
    public partial class FixMallCartUniqueIndexFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MallCarts_UserId",
                table: "MallCarts");

            migrationBuilder.CreateIndex(
                name: "IX_MallCarts_UserId",
                table: "MallCarts",
                column: "UserId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.DropIndex(
                name: "IX_MallCartItems_CartId_SkuId",
                table: "MallCartItems");

            migrationBuilder.CreateIndex(
                name: "IX_MallCartItems_CartId_SkuId",
                table: "MallCartItems",
                columns: new[] { "CartId", "SkuId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MallCarts_UserId",
                table: "MallCarts");

            migrationBuilder.CreateIndex(
                name: "IX_MallCarts_UserId",
                table: "MallCarts",
                column: "UserId",
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_MallCartItems_CartId_SkuId",
                table: "MallCartItems");

            migrationBuilder.CreateIndex(
                name: "IX_MallCartItems_CartId_SkuId",
                table: "MallCartItems",
                columns: new[] { "CartId", "SkuId" },
                unique: true);
        }
    }
}
