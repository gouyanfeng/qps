using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.src.QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResetNonAmountPromotionThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE [MallPromotions] SET [ThresholdAmount] = 0 WHERE [ActionType] <> N'DiscountAmount' AND [ThresholdAmount] <> 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}