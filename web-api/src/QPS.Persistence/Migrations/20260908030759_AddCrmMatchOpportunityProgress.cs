using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmMatchOpportunityProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NextActionRemark",
                table: "CrmMatchOpportunities",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "CrmMatchOpportunities",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE dbo.CrmMatchOpportunities
                SET Stage = N'匹配核实',
                    NextActionRemark = CASE
                        WHEN Status IN (N'推进中', N'暂停') AND NULLIF(LTRIM(RTRIM(NextAction)), N'') IS NOT NULL THEN NextAction
                        WHEN Status IN (N'推进中', N'暂停') THEN N'历史待办缺失'
                        ELSE N''
                    END,
                    NextAction = CASE WHEN Status IN (N'推进中', N'暂停') THEN N'其他' ELSE NextAction END
                WHERE IsDeleted = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextActionRemark",
                table: "CrmMatchOpportunities");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "CrmMatchOpportunities");
        }
    }
}
