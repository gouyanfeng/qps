using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCrmMatchOpportunities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrmBusinessActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmBusinessActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrmMatchOpportunities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DemandItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DemandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DemandProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DemandQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuantityUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Specification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QualityRequirement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpectedDeliveryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NextAction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NextActionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmMatchOpportunities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmMatchOpportunities_CrmVendorDemandItems_DemandItemId",
                        column: x => x.DemandItemId,
                        principalTable: "CrmVendorDemandItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrmMatchCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchOpportunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbBaseSupplyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbBaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbBaseSubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HerbBaseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuantityUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Specification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QualityRequirement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpectedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PriceUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SupplyCycle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EliminationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmMatchCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmMatchCandidates_CrmHerbBaseSupplies_HerbBaseSupplyId",
                        column: x => x.HerbBaseSupplyId,
                        principalTable: "CrmHerbBaseSupplies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmMatchCandidates_CrmMatchOpportunities_MatchOpportunityId",
                        column: x => x.MatchOpportunityId,
                        principalTable: "CrmMatchOpportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrmBusinessActivities_EntityType_EntityId_CreatedAt",
                table: "CrmBusinessActivities",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchCandidates_HerbBaseSupplyId",
                table: "CrmMatchCandidates",
                column: "HerbBaseSupplyId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchCandidates_MatchOpportunityId_HerbBaseSupplyId",
                table: "CrmMatchCandidates",
                columns: new[] { "MatchOpportunityId", "HerbBaseSupplyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchOpportunities_DemandItemId_Status",
                table: "CrmMatchOpportunities",
                columns: new[] { "DemandItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchOpportunities_OwnerUserId_NextActionAt",
                table: "CrmMatchOpportunities",
                columns: new[] { "OwnerUserId", "NextActionAt" });

            migrationBuilder.Sql(@"
                DECLARE @rootId uniqueidentifier = (SELECT TOP (1) Id FROM dbo.SystemPermissions WHERE Code = 'ROOT' AND IsDeleted = 0);
                DECLARE @viewId uniqueidentifier = (SELECT TOP (1) Id FROM dbo.SystemPermissions WHERE Code = 'CRM_MATCH_OPPORTUNITY_VIEW' AND IsDeleted = 0);
                IF @viewId IS NULL BEGIN
                    SET @viewId = NEWID();
                    INSERT INTO dbo.SystemPermissions (Id, Name, Code, ParentId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
                    VALUES (@viewId, N'查看撮合机会', 'CRM_MATCH_OPPORTUNITY_VIEW', @rootId, GETDATE(), 'migration', GETDATE(), 'migration', 0);
                END;
                DECLARE @manageId uniqueidentifier = (SELECT TOP (1) Id FROM dbo.SystemPermissions WHERE Code = 'CRM_MATCH_OPPORTUNITY_MANAGE' AND IsDeleted = 0);
                IF @manageId IS NULL BEGIN
                    SET @manageId = NEWID();
                    INSERT INTO dbo.SystemPermissions (Id, Name, Code, ParentId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
                    VALUES (@manageId, N'维护撮合机会', 'CRM_MATCH_OPPORTUNITY_MANAGE', @rootId, GETDATE(), 'migration', GETDATE(), 'migration', 0);
                END;
                INSERT INTO dbo.SystemRolePermissions (Id, RoleId, PermissionId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
                SELECT NEWID(), role.Id, permission.Id, GETDATE(), 'migration', GETDATE(), 'migration', 0
                FROM dbo.SystemRoles role CROSS JOIN dbo.SystemPermissions permission
                WHERE role.Code = 'admin' AND role.IsDeleted = 0 AND permission.Code IN ('CRM_MATCH_OPPORTUNITY_VIEW', 'CRM_MATCH_OPPORTUNITY_MANAGE') AND permission.IsDeleted = 0
                  AND NOT EXISTS (SELECT 1 FROM dbo.SystemRolePermissions assigned WHERE assigned.RoleId = role.Id AND assigned.PermissionId = permission.Id AND assigned.IsDeleted = 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE assigned FROM dbo.SystemRolePermissions assigned
                INNER JOIN dbo.SystemPermissions permission ON permission.Id = assigned.PermissionId
                WHERE permission.Code IN ('CRM_MATCH_OPPORTUNITY_VIEW', 'CRM_MATCH_OPPORTUNITY_MANAGE');
                DELETE FROM dbo.SystemPermissions WHERE Code IN ('CRM_MATCH_OPPORTUNITY_VIEW', 'CRM_MATCH_OPPORTUNITY_MANAGE');
            ");

            migrationBuilder.DropTable(
                name: "CrmBusinessActivities");

            migrationBuilder.DropTable(
                name: "CrmMatchCandidates");

            migrationBuilder.DropTable(
                name: "CrmMatchOpportunities");
        }
    }
}
