using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <summary>
    /// 订单快照补齐（一次迁移完成加列、改约束、删列与存量回填）：
    /// - MallOrderItems.PromotionDiscountAmount：本行实际促销优惠额，下单由促销引擎结果写入；
    /// - MallOrderItems.PromotionSnapshot：本行命中的促销结构化快照（JSON），展示层自行拼文案；
    /// - MallOrderItems.OriginalUnitPrice：改为非空恒写，无促销时等于 UnitPrice；
    /// - MallOrders.CouponId / CouponName：下单核销券时写入的券快照；
    /// - MallOrders.AddressId：始终为空的历史列，直接删除。
    ///
    /// 存量回填口径：
    /// 1) OriginalUnitPrice 为 NULL 的明细补成 UnitPrice（历史无促销行未记录原价）。
    /// 2) 行级促销额 = (OriginalUnitPrice − UnitPrice) × Quantity，赠品与负数差额按 0 处理。
    /// 3) PromotionSnapshot 仅按已有的 PromotionId / PromotionName 生成单条快照：
    ///    非赠品 actionType 记为 "Unknown"（历史数据未记录动作类型），赠品记为 "Gift"、amount 为 0；
    ///    历史 PromotionName 可能是「名称(-金额)」的拼接文案，快照 name 保留原文，展示层不再依赖该字段。
    /// 4) 券快照按 MallMemberCoupons.UsedOrderNo 关联订单号回填券模板 ID 与名称；
    ///    券记录已删除或找不到对应用户券时留空，不回填推测值。
    /// </summary>
    [Migration("20261107050000_CompleteMallOrderSnapshots")]
    public partial class CompleteMallOrderSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CouponId",
                table: "MallOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CouponName",
                table: "MallOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PromotionDiscountAmount",
                table: "MallOrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PromotionSnapshot",
                table: "MallOrderItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql(
                "UPDATE MallOrderItems SET OriginalUnitPrice = UnitPrice WHERE OriginalUnitPrice IS NULL;");

            migrationBuilder.AlterColumn<decimal>(
                name: "OriginalUnitPrice",
                table: "MallOrderItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            // 行级促销额：按原价快照与成交单价差回填，赠品与负差额按 0 处理
            migrationBuilder.Sql(
                "UPDATE MallOrderItems SET PromotionDiscountAmount = " +
                "CASE WHEN IsGift = 1 OR OriginalUnitPrice <= UnitPrice THEN CAST(0 AS decimal(18,2)) " +
                "ELSE CAST((OriginalUnitPrice - UnitPrice) * Quantity AS decimal(18,2)) END;");

            // 结构化促销快照：历史仅有单一促销来源与名称，动作类型未知，名称保留原文
            migrationBuilder.Sql(
                "UPDATE MallOrderItems SET PromotionSnapshot = " +
                "'[{\"id\":\"' + LOWER(CONVERT(varchar(36), PromotionId)) + '\",\"name\":\"' + " +
                "STRING_ESCAPE(PromotionName, 'json') + '\",\"actionType\":\"' + " +
                "CASE WHEN IsGift = 1 THEN 'Gift' ELSE 'Unknown' END + '\",\"amount\":' + " +
                "CONVERT(varchar(32), PromotionDiscountAmount) + '}]' " +
                "WHERE PromotionId IS NOT NULL;");

            // 券快照：按券核销记录回填；券模板已删除时仍取到名称（按 Id 直连，不套软删除过滤）
            migrationBuilder.Sql(
                "UPDATE o SET o.CouponId = cov.CouponId, o.CouponName = cov.CouponName " +
                "FROM MallOrders AS o " +
                "CROSS APPLY ( " +
                "SELECT TOP 1 c.Id AS CouponId, c.Name AS CouponName " +
                "FROM MallMemberCoupons AS mc " +
                "JOIN MallCoupons AS c ON c.Id = mc.CouponId " +
                "WHERE mc.UsedOrderNo = o.OrderNo " +
                "ORDER BY mc.UsedAt DESC) AS cov;");

            migrationBuilder.DropForeignKey(
                name: "FK_MallOrders_MallAddresses_AddressId",
                table: "MallOrders");

            migrationBuilder.DropIndex(
                name: "IX_MallOrders_AddressId",
                table: "MallOrders");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "MallOrders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AddressId",
                table: "MallOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "OriginalUnitPrice",
                table: "MallOrderItems",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.DropColumn(
                name: "PromotionDiscountAmount",
                table: "MallOrderItems");

            migrationBuilder.DropColumn(
                name: "PromotionSnapshot",
                table: "MallOrderItems");

            migrationBuilder.DropColumn(
                name: "CouponId",
                table: "MallOrders");

            migrationBuilder.DropColumn(
                name: "CouponName",
                table: "MallOrders");

            migrationBuilder.CreateIndex(
                name: "IX_MallOrders_AddressId",
                table: "MallOrders",
                column: "AddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_MallOrders_MallAddresses_AddressId",
                table: "MallOrders",
                column: "AddressId",
                principalTable: "MallAddresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
