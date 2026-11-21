using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchOpportunityDemands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "CrmMatchCandidates",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "candidate");

            migrationBuilder.CreateTable(
                name: "CrmMatchOpportunityDemands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DemandItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuantityUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Specification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QualityRequirement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TargetPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PriceUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExpectedDeliveryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
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
                    table.PrimaryKey("PK_CrmMatchOpportunityDemands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmMatchOpportunityDemands_CrmMatchOpportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "CrmMatchOpportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchOpportunityDemands_OpportunityId",
                table: "CrmMatchOpportunityDemands",
                column: "OpportunityId");
            migrationBuilder.Sql(@"
INSERT INTO dbo.CrmMatchOpportunityDemands
 (Id, OpportunityId, DemandItemId, VendorId, VendorName, ProductName, Quantity, QuantityUnit,
  Specification, QualityRequirement, TargetPrice, PriceUnit, ExpectedDeliveryAt, DeliveryAddress,
  Role, Status, EliminationReason, SortOrder, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
SELECT NEWID(), o.Id, di.Id, vd.VendorId, ISNULL(v.VendorName,''), ISNULL(di.ProductName,''),
       di.Quantity, ISNULL(di.QuantityUnit,''), ISNULL(di.Specification,''), ISNULL(di.QualityRequirement,''),
       di.TargetPrice, ISNULL(di.PriceUnit,''), ISNULL(di.ExpectedDeliveryAt,o.ExpectedDeliveryAt),
       ISNULL(o.DeliveryAddress,''),
       CASE WHEN o.Source='supply' THEN 'candidate' ELSE 'initiator' END,
       'pending', '', 1, GETDATE(), 'migration', GETDATE(), 'migration', 0
FROM dbo.CrmMatchOpportunities o
JOIN dbo.CrmVendorDemandItems di ON di.Id=o.DemandItemId
JOIN dbo.CrmVendorDemands vd ON vd.Id=di.VendorDemandId
LEFT JOIN dbo.CrmVendors v ON v.Id=vd.VendorId
WHERE NOT EXISTS (SELECT 1 FROM dbo.CrmMatchOpportunityDemands x WHERE x.OpportunityId=o.Id AND x.DemandItemId=di.Id);

UPDATE c SET Role='initiator'
FROM dbo.CrmMatchCandidates c
JOIN dbo.CrmMatchOpportunities o ON o.Id=c.MatchOpportunityId
WHERE o.Source='supply';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrmMatchOpportunityDemands");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "CrmMatchCandidates");
        }
    }
}
