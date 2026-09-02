using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260902113000_BackfillCrmHerbBaseSupplies")]
public partial class BackfillCrmHerbBaseSupplies : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            INSERT INTO dbo.CrmHerbBaseSupplies
                (Id, HerbBaseId, HerbBaseSubjectId, ProductName, AvailableQuantity, QuantityUnit, Specification, QualityRequirement, HarvestSeason, ExpectedPrice, PriceUnit, SupplyCycle, ConfirmedAt, ValidUntil, Status, Remark, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted)
            SELECT NEWID(), base.Id, base.HerbBaseSubjectId, attribute.AttributeValue, NULL, N'', N'', N'', N'', NULL, N'', N'', NULL, NULL, N'待确认', N'', GETDATE(), 'migration', GETDATE(), 'migration', 0
            FROM dbo.CrmHerbBases base
            INNER JOIN dbo.CrmBusinessEntityAttributes attribute ON attribute.EntityId = base.Id
            WHERE base.IsDeleted = 0
              AND attribute.IsDeleted = 0
              AND attribute.EntityType = 'CRM_HERB_BASE'
              AND attribute.AttributeCode = 'CRM_MAIN_PRODUCT'
              AND LTRIM(RTRIM(attribute.AttributeValue)) <> ''
              AND NOT EXISTS (
                  SELECT 1
                  FROM dbo.CrmHerbBaseSupplies supply
                  WHERE supply.HerbBaseId = base.Id
                    AND supply.ProductName = attribute.AttributeValue
                    AND supply.IsDeleted = 0);
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
