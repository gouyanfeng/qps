using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <summary>
    /// 订单展示快照补齐：
    /// - MallOrders.PromotionDiscountAmount：促销优惠在下单时固化，列表与详情读取同一字段；
    /// - MallOrderItems.ProductImage：商品主图下单时快照，展示不再回查商品表。
    /// 存量数据按订单明细快照与商品表各回填一次。
    /// </summary>
    [Migration("20261107030000_AddMallOrderSnapshotFields")]
    public partial class AddMallOrderSnapshotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PromotionDiscountAmount",
                table: "MallOrders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProductImage",
                table: "MallOrderItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            // 存量订单：促销优惠 = 商品原价 − 明细实付合计（明细均为下单时快照）
            migrationBuilder.Sql(
                "UPDATE o SET o.PromotionDiscountAmount = o.GoodsAmount - ISNULL(" +
                "(SELECT SUM(i.ItemAmount) FROM MallOrderItems AS i WHERE i.OrderId = o.Id), 0) " +
                "FROM MallOrders AS o;");

            // 存量明细：商品图按当前商品表回填一次，之后只读快照
            migrationBuilder.Sql(
                "UPDATE i SET i.ProductImage = ISNULL(p.MainImageUrl, '') " +
                "FROM MallOrderItems AS i LEFT JOIN MallProducts AS p ON p.Id = i.ProductId;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PromotionDiscountAmount",
                table: "MallOrders");

            migrationBuilder.DropColumn(
                name: "ProductImage",
                table: "MallOrderItems");
        }
    }
}
