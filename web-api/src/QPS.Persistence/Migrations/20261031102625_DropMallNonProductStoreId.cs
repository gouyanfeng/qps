using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropMallNonProductStoreId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MallFreightTemplates_MallStores_StoreId",
                table: "MallFreightTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_MallOrders_MallStores_StoreId",
                table: "MallOrders");

            migrationBuilder.DropIndex(
                name: "IX_MallPages_StoreId_PageCode",
                table: "MallPages");

            migrationBuilder.DropIndex(
                name: "IX_MallOrders_StoreId_Status",
                table: "MallOrders");

            migrationBuilder.DropIndex(
                name: "IX_MallNotices_StoreId_IsTop_SortOrder",
                table: "MallNotices");

            migrationBuilder.DropIndex(
                name: "IX_MallMembers_StoreId",
                table: "MallMembers");

            migrationBuilder.DropIndex(
                name: "IX_MallMemberLevels_StoreId_IsActive",
                table: "MallMemberLevels");

            migrationBuilder.DropIndex(
                name: "IX_MallMemberLevels_StoreId_LevelCode",
                table: "MallMemberLevels");

            migrationBuilder.DropIndex(
                name: "IX_MallFreightTemplates_StoreId",
                table: "MallFreightTemplates");

            migrationBuilder.DropIndex(
                name: "IX_MallCoupons_StoreId_Code",
                table: "MallCoupons");

            migrationBuilder.DropIndex(
                name: "IX_MallCoupons_StoreId_IsActive",
                table: "MallCoupons");

            migrationBuilder.DropIndex(
                name: "IX_MallCarts_UserId_StoreId",
                table: "MallCarts");

            migrationBuilder.DropIndex(
                name: "IX_MallBanners_StoreId_Position_SortOrder",
                table: "MallBanners");

            migrationBuilder.DropIndex(
                name: "IX_MallArticles_StoreId_ArticleType_IsActive_SortOrder",
                table: "MallArticles");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MallPages");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MallOrders");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MallNotices");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MallMembers");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MallMemberLevels");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MallFreightTemplates");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MallCoupons");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MallCategories");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MallCarts");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MallBanners");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MallArticles");

            migrationBuilder.CreateIndex(
                name: "IX_MallPages_PageCode",
                table: "MallPages",
                column: "PageCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallOrders_Status",
                table: "MallOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MallNotices_IsTop_SortOrder",
                table: "MallNotices",
                columns: new[] { "IsTop", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MallMemberLevels_IsActive",
                table: "MallMemberLevels",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MallMemberLevels_LevelCode",
                table: "MallMemberLevels",
                column: "LevelCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallCoupons_Code",
                table: "MallCoupons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallCoupons_IsActive",
                table: "MallCoupons",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MallCarts_UserId",
                table: "MallCarts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallBanners_Position_SortOrder",
                table: "MallBanners",
                columns: new[] { "Position", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallArticles_ArticleType_IsActive_SortOrder",
                table: "MallArticles",
                columns: new[] { "ArticleType", "IsActive", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MallPages_PageCode",
                table: "MallPages");

            migrationBuilder.DropIndex(
                name: "IX_MallOrders_Status",
                table: "MallOrders");

            migrationBuilder.DropIndex(
                name: "IX_MallNotices_IsTop_SortOrder",
                table: "MallNotices");

            migrationBuilder.DropIndex(
                name: "IX_MallMemberLevels_IsActive",
                table: "MallMemberLevels");

            migrationBuilder.DropIndex(
                name: "IX_MallMemberLevels_LevelCode",
                table: "MallMemberLevels");

            migrationBuilder.DropIndex(
                name: "IX_MallCoupons_Code",
                table: "MallCoupons");

            migrationBuilder.DropIndex(
                name: "IX_MallCoupons_IsActive",
                table: "MallCoupons");

            migrationBuilder.DropIndex(
                name: "IX_MallCarts_UserId",
                table: "MallCarts");

            migrationBuilder.DropIndex(
                name: "IX_MallBanners_Position_SortOrder",
                table: "MallBanners");

            migrationBuilder.DropIndex(
                name: "IX_MallArticles_ArticleType_IsActive_SortOrder",
                table: "MallArticles");

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MallPages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MallOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MallNotices",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MallMembers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MallMemberLevels",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MallFreightTemplates",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MallCoupons",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MallCategories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MallCarts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MallBanners",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MallArticles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_MallPages_StoreId_PageCode",
                table: "MallPages",
                columns: new[] { "StoreId", "PageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallOrders_StoreId_Status",
                table: "MallOrders",
                columns: new[] { "StoreId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MallNotices_StoreId_IsTop_SortOrder",
                table: "MallNotices",
                columns: new[] { "StoreId", "IsTop", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MallMembers_StoreId",
                table: "MallMembers",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_MallMemberLevels_StoreId_IsActive",
                table: "MallMemberLevels",
                columns: new[] { "StoreId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MallMemberLevels_StoreId_LevelCode",
                table: "MallMemberLevels",
                columns: new[] { "StoreId", "LevelCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallFreightTemplates_StoreId",
                table: "MallFreightTemplates",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_MallCoupons_StoreId_Code",
                table: "MallCoupons",
                columns: new[] { "StoreId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallCoupons_StoreId_IsActive",
                table: "MallCoupons",
                columns: new[] { "StoreId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MallCarts_UserId_StoreId",
                table: "MallCarts",
                columns: new[] { "UserId", "StoreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallBanners_StoreId_Position_SortOrder",
                table: "MallBanners",
                columns: new[] { "StoreId", "Position", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallArticles_StoreId_ArticleType_IsActive_SortOrder",
                table: "MallArticles",
                columns: new[] { "StoreId", "ArticleType", "IsActive", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_MallFreightTemplates_MallStores_StoreId",
                table: "MallFreightTemplates",
                column: "StoreId",
                principalTable: "MallStores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MallOrders_MallStores_StoreId",
                table: "MallOrders",
                column: "StoreId",
                principalTable: "MallStores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
