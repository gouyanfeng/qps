using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMallProductStoreId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MallProducts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // 历史商品归属到现存门店，避免新增非空列后无法建立外键
            migrationBuilder.Sql(
                "IF EXISTS (SELECT 1 FROM [MallStores] WHERE [IsDeleted] = 0) " +
                "BEGIN " +
                "UPDATE [MallProducts] SET [StoreId] = (SELECT TOP (1) [Id] FROM [MallStores] WHERE [IsDeleted] = 0 ORDER BY [CreatedAt]) " +
                "WHERE [StoreId] = '00000000-0000-0000-0000-000000000000'; " +
                "END");

            migrationBuilder.CreateIndex(
                name: "IX_MallProducts_StoreId",
                table: "MallProducts",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_MallProducts_MallStores_StoreId",
                table: "MallProducts",
                column: "StoreId",
                principalTable: "MallStores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MallProducts_MallStores_StoreId",
                table: "MallProducts");

            migrationBuilder.DropIndex(
                name: "IX_MallProducts_StoreId",
                table: "MallProducts");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MallProducts");
        }
    }
}
