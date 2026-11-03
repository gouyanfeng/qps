using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMallProductReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MallProductReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberNickname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MemberLevelName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SpecText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReviewTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallProductReviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MallProductReviews_ProductId_ReviewTime",
                table: "MallProductReviews",
                columns: new[] { "ProductId", "ReviewTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MallProductReviews");
        }
    }
}
