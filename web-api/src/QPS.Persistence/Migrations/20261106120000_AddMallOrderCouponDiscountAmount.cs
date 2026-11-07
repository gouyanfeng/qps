using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMallOrderCouponDiscountAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CouponDiscountAmount",
                table: "MallOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // 券抵扣 = 促销后金额（明细单价合计）− 迁移前已扣除券的商品金额，负值归零
            migrationBuilder.Sql(
                """
                UPDATE MallOrders
                SET CouponDiscountAmount = CASE
                    WHEN items.PromotionAfterAmount - MallOrders.GoodsAmount > 0
                        THEN CAST(ROUND(items.PromotionAfterAmount - MallOrders.GoodsAmount, 2) AS decimal(18,2))
                    ELSE CAST(0 AS decimal(18,2))
                END
                FROM MallOrders
                INNER JOIN (
                    SELECT OrderId,
                           SUM(CAST(UnitPrice AS decimal(18,2)) * Quantity) AS PromotionAfterAmount
                    FROM MallOrderItems
                    WHERE IsGift = 0 AND IsDeleted = 0
                    GROUP BY OrderId
                ) AS items ON items.OrderId = MallOrders.Id;
                """);

            // 商品金额统一为促销前原价合计：优先取明细原价快照，无快照时取下单单价
            migrationBuilder.Sql(
                """
                UPDATE MallOrders
                SET GoodsAmount = items.OriginalAmount
                FROM MallOrders
                INNER JOIN (
                    SELECT OrderId,
                           SUM(CAST(ISNULL(OriginalUnitPrice, UnitPrice) AS decimal(18,2)) * Quantity) AS OriginalAmount
                    FROM MallOrderItems
                    WHERE IsGift = 0 AND IsDeleted = 0
                    GROUP BY OrderId
                ) AS items ON items.OrderId = MallOrders.Id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 回退为“促销后金额 − 券抵扣”的旧口径
            migrationBuilder.Sql(
                """
                UPDATE MallOrders
                SET GoodsAmount = items.OriginalAmount - items.PromotionDiscountAmount - MallOrders.CouponDiscountAmount
                FROM MallOrders
                INNER JOIN (
                    SELECT OrderId,
                           SUM(CAST(ISNULL(OriginalUnitPrice, UnitPrice) AS decimal(18,2)) * Quantity) AS OriginalAmount,
                           SUM((CAST(ISNULL(OriginalUnitPrice, UnitPrice) AS decimal(18,2)) - UnitPrice) * Quantity) AS PromotionDiscountAmount
                    FROM MallOrderItems
                    WHERE IsGift = 0 AND IsDeleted = 0
                    GROUP BY OrderId
                ) AS items ON items.OrderId = MallOrders.Id;
                """);

            migrationBuilder.DropColumn(
                name: "CouponDiscountAmount",
                table: "MallOrders");
        }
    }
}
