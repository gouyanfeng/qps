using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class BackfillLegacyCrmVendorDemandMatchProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "匹配中"曾被用于表示撮合机会进度，现已由机会记录派生，需求状态统一保持“有效”。
            migrationBuilder.Sql("UPDATE dbo.CrmVendorDemands SET Status = N'有效' WHERE Status = N'匹配中';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
