using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMallProductReviewAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AuditStatus",
                table: "MallProductReviews",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "ReplyContent",
                table: "MallProductReviews",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuditStatus",
                table: "MallProductReviews");

            migrationBuilder.DropColumn(
                name: "ReplyContent",
                table: "MallProductReviews");
        }
    }
}
