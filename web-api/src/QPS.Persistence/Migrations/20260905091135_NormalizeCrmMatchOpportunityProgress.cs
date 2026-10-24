using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeCrmMatchOpportunityProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE dbo.CrmVendorDemands SET Status = N'有效' WHERE Status = N'匹配中';");

            migrationBuilder.Sql(@"
                DECLARE @closedOpportunities TABLE
                (
                    Id uniqueidentifier NOT NULL,
                    ChangedAt datetime2 NOT NULL
                );

                ;WITH rankedOpportunities AS
                (
                    SELECT
                        Id,
                        Status,
                        ClosedReason,
                        UpdatedAt,
                        UpdatedBy,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY DemandItemId
                            ORDER BY UpdatedAt DESC, CreatedAt DESC, Id DESC
                        ) AS RowNumber
                    FROM dbo.CrmMatchOpportunities
                    WHERE IsDeleted = 0
                      AND Status IN (N'推进中', N'暂停')
                )
                UPDATE rankedOpportunities
                SET
                    Status = N'已关闭',
                    ClosedReason = N'系统迁移：同一采购明细仅保留最新未关闭机会',
                    UpdatedAt = SYSDATETIME(),
                    UpdatedBy = N'SYSTEM_MIGRATION'
                OUTPUT INSERTED.Id, INSERTED.UpdatedAt INTO @closedOpportunities
                WHERE RowNumber > 1;

                INSERT INTO dbo.CrmBusinessActivities
                (
                    Id,
                    EntityType,
                    EntityId,
                    ActionType,
                    Content,
                    CreatedAt,
                    CreatedBy,
                    UpdatedAt,
                    UpdatedBy,
                    IsDeleted
                )
                SELECT
                    NEWID(),
                    N'MATCH_OPPORTUNITY',
                    Id,
                    N'系统迁移关闭',
                    N'系统迁移：同一采购明细仅保留最新未关闭机会，已关闭本记录',
                    ChangedAt,
                    N'SYSTEM_MIGRATION',
                    ChangedAt,
                    N'SYSTEM_MIGRATION',
                    0
                FROM @closedOpportunities;
            ");

            migrationBuilder.DropIndex(
                name: "IX_CrmMatchOpportunities_DemandItemId_Status",
                table: "CrmMatchOpportunities");

            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchOpportunities_DemandItemId",
                table: "CrmMatchOpportunities",
                column: "DemandItemId",
                unique: true,
                filter: "[Status] IN (N'推进中', N'暂停') AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CrmMatchOpportunities_DemandItemId",
                table: "CrmMatchOpportunities");

            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchOpportunities_DemandItemId_Status",
                table: "CrmMatchOpportunities",
                columns: new[] { "DemandItemId", "Status" });
        }
    }
}
