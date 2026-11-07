using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMallProductReviewOrderLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Images",
                table: "MallProductReviews",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "MallProductReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "MemberId",
                table: "MallProductReviews",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "MallProductReviews",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrderItemId",
                table: "MallProductReviews",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "MallProductReviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MallProductReviews_OrderItemId",
                table: "MallProductReviews",
                column: "OrderItemId",
                unique: true,
                filter: "[OrderItemId] IS NOT NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MallProductReviews_OrderItemId",
                table: "MallProductReviews");

            migrationBuilder.DropColumn(
                name: "Images",
                table: "MallProductReviews");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "MallProductReviews");

            migrationBuilder.DropColumn(
                name: "MemberId",
                table: "MallProductReviews");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "MallProductReviews");

            migrationBuilder.DropColumn(
                name: "OrderItemId",
                table: "MallProductReviews");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "MallProductReviews");
        }
    }
}
