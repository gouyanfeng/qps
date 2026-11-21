using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MergeMatchOpportunityItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) 先建新子表（此时不建唯一索引，避免搬迁数据因历史重复行冲突）
            migrationBuilder.CreateTable(
                name: "CrmMatchOpportunityItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "candidate"),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "pending"),
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
                    table.PrimaryKey("PK_CrmMatchOpportunityItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmMatchOpportunityItems_CrmMatchOpportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "CrmMatchOpportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 2) 搬迁旧两表数据到新子表。
            //    供应侧 Status 中文映射为英文；需求侧已是英文。
            //    同一 (机会, 明细) 有软删重复历史行，只取一条（优先有效行，其次最早创建）。
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

-- 供应候选 -> supply 类型 item
INSERT INTO CrmMatchOpportunityItems
    (Id, OpportunityId, EntityType, EntityId, Role, Status, EliminationReason, SortOrder,
     CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
SELECT c.Id, c.MatchOpportunityId, N'supply', c.HerbBaseSupplyId, c.Role,
       CASE c.Status
           WHEN N'待评估' THEN 'pending'
           WHEN N'保留' THEN 'kept'
           WHEN N'中选' THEN 'selected'
           WHEN N'淘汰' THEN 'eliminated'
           ELSE 'pending'
       END,
       c.EliminationReason, c.SortOrder,
       c.CreatedAt, c.CreatedBy, c.UpdatedAt, c.UpdatedBy, c.IsDeleted
FROM CrmMatchCandidates c
WHERE NOT EXISTS (
    SELECT 1 FROM CrmMatchCandidates d
    WHERE d.MatchOpportunityId = c.MatchOpportunityId
      AND d.HerbBaseSupplyId = c.HerbBaseSupplyId
      AND (
        (d.IsDeleted = 0 AND c.IsDeleted = 1)
        OR (d.IsDeleted = c.IsDeleted AND d.CreatedAt < c.CreatedAt)
        OR (d.IsDeleted = c.IsDeleted AND d.CreatedAt = c.CreatedAt AND d.Id < c.Id)
      )
);

-- 需求候选 -> demand 类型 item
INSERT INTO CrmMatchOpportunityItems
    (Id, OpportunityId, EntityType, EntityId, Role, Status, EliminationReason, SortOrder,
     CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
SELECT d.Id, d.OpportunityId, N'demand', d.DemandItemId, d.Role,
       CASE d.Status
           WHEN 'pending' THEN 'pending'
           WHEN 'kept' THEN 'kept'
           WHEN 'selected' THEN 'selected'
           WHEN 'eliminated' THEN 'eliminated'
           ELSE 'pending'
       END,
       d.EliminationReason, d.SortOrder,
       d.CreatedAt, d.CreatedBy, d.UpdatedAt, d.UpdatedBy, d.IsDeleted
FROM CrmMatchOpportunityDemands d
WHERE NOT EXISTS (
    SELECT 1 FROM CrmMatchOpportunityDemands x
    WHERE x.OpportunityId = d.OpportunityId
      AND x.DemandItemId = d.DemandItemId
      AND (
        (x.IsDeleted = 0 AND d.IsDeleted = 1)
        OR (x.IsDeleted = d.IsDeleted AND x.CreatedAt < d.CreatedAt)
        OR (x.IsDeleted = d.IsDeleted AND x.CreatedAt = d.CreatedAt AND x.Id < d.Id)
      )
);

-- 极端情况：一个机会多个 initiator 时只保留 SortOrder 最小的一条，其余降级为 candidate，保证唯一索引可建。
WITH ranked AS (
    SELECT Id, ROW_NUMBER() OVER (PARTITION BY OpportunityId, Role ORDER BY SortOrder, CreatedAt) AS rn
    FROM CrmMatchOpportunityItems
    WHERE Role = 'initiator' AND IsDeleted = 0
)
UPDATE i SET Role = 'candidate'
FROM CrmMatchOpportunityItems i
JOIN ranked r ON i.Id = r.Id
WHERE r.rn > 1;
");

            // 3) 数据搬迁完成，建唯一索引
            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchOpportunityItems_OpportunityId",
                table: "CrmMatchOpportunityItems",
                column: "OpportunityId",
                unique: true,
                filter: "[Role] = 'initiator' AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchOpportunityItems_OpportunityId_EntityType_EntityId",
                table: "CrmMatchOpportunityItems",
                columns: new[] { "OpportunityId", "EntityType", "EntityId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            // 4) 删除旧表与主表快照列
            migrationBuilder.DropForeignKey(
                name: "FK_CrmMatchOpportunities_CrmVendorDemandItems_DemandItemId",
                table: "CrmMatchOpportunities");

            migrationBuilder.DropTable(name: "CrmMatchCandidates");
            migrationBuilder.DropTable(name: "CrmMatchOpportunityDemands");

            migrationBuilder.DropIndex(
                name: "IX_CrmMatchOpportunities_DemandItemId",
                table: "CrmMatchOpportunities");

            migrationBuilder.DropColumn(name: "DeliveryAddress", table: "CrmMatchOpportunities");
            migrationBuilder.DropColumn(name: "DemandId", table: "CrmMatchOpportunities");
            migrationBuilder.DropColumn(name: "DemandItemId", table: "CrmMatchOpportunities");
            migrationBuilder.DropColumn(name: "DemandProductName", table: "CrmMatchOpportunities");
            migrationBuilder.DropColumn(name: "DemandQuantity", table: "CrmMatchOpportunities");
            migrationBuilder.DropColumn(name: "ExpectedDeliveryAt", table: "CrmMatchOpportunities");
            migrationBuilder.DropColumn(name: "QualityRequirement", table: "CrmMatchOpportunities");
            migrationBuilder.DropColumn(name: "QuantityUnit", table: "CrmMatchOpportunities");
            migrationBuilder.DropColumn(name: "Specification", table: "CrmMatchOpportunities");
            migrationBuilder.DropColumn(name: "VendorId", table: "CrmMatchOpportunities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "CrmMatchOpportunityItems");

            migrationBuilder.AddColumn<string>(name: "DeliveryAddress", table: "CrmMatchOpportunities", type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<Guid>(name: "DemandId", table: "CrmMatchOpportunities", type: "uniqueidentifier", nullable: false, defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
            migrationBuilder.AddColumn<Guid>(name: "DemandItemId", table: "CrmMatchOpportunities", type: "uniqueidentifier", nullable: true);
            migrationBuilder.AddColumn<string>(name: "DemandProductName", table: "CrmMatchOpportunities", type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<decimal>(name: "DemandQuantity", table: "CrmMatchOpportunities", type: "decimal(18,2)", precision: 18, scale: 2, nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "ExpectedDeliveryAt", table: "CrmMatchOpportunities", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "QualityRequirement", table: "CrmMatchOpportunities", type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "QuantityUnit", table: "CrmMatchOpportunities", type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "Specification", table: "CrmMatchOpportunities", type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<Guid>(name: "VendorId", table: "CrmMatchOpportunities", type: "uniqueidentifier", nullable: false, defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "CrmMatchCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EliminationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpectedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    HerbBaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbBaseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HerbBaseSubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HerbBaseSupplyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    MatchOpportunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriceUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QualityRequirement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QuantityUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "candidate"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Specification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SubjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SupplyCycle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmMatchCandidates", x => x.Id);
                    table.ForeignKey(name: "FK_CrmMatchCandidates_CrmHerbBaseSupplies_HerbBaseSupplyId", column: x => x.HerbBaseSupplyId, principalTable: "CrmHerbBaseSupplies", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(name: "FK_CrmMatchCandidates_CrmMatchOpportunities_MatchOpportunityId", column: x => x.MatchOpportunityId, principalTable: "CrmMatchOpportunities", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrmMatchOpportunityDemands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeliveryAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DemandItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EliminationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpectedDeliveryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    OpportunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PriceUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QualityRequirement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuantityUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Specification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    TargetPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmMatchOpportunityDemands", x => x.Id);
                    table.ForeignKey(name: "FK_CrmMatchOpportunityDemands_CrmMatchOpportunities_OpportunityId", column: x => x.OpportunityId, principalTable: "CrmMatchOpportunities", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(name: "IX_CrmMatchOpportunities_DemandItemId", table: "CrmMatchOpportunities", column: "DemandItemId", unique: true, filter: "[DemandItemId] IS NOT NULL AND [Status] IN (N'推进中', N'暂停') AND [IsDeleted] = 0");
            migrationBuilder.CreateIndex(name: "IX_CrmMatchCandidates_HerbBaseSupplyId", table: "CrmMatchCandidates", column: "HerbBaseSupplyId");
            migrationBuilder.CreateIndex(name: "IX_CrmMatchCandidates_MatchOpportunityId_HerbBaseSupplyId", table: "CrmMatchCandidates", columns: new[] { "MatchOpportunityId", "HerbBaseSupplyId" }, unique: true, filter: "[IsDeleted] = 0");
            migrationBuilder.CreateIndex(name: "IX_CrmMatchOpportunityDemands_OpportunityId", table: "CrmMatchOpportunityDemands", column: "OpportunityId");
            migrationBuilder.AddForeignKey(name: "FK_CrmMatchOpportunities_CrmVendorDemandItems_DemandItemId", table: "CrmMatchOpportunities", column: "DemandItemId", principalTable: "CrmVendorDemandItems", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        }
    }
}
