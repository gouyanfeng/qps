using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QPS.Infrastructure.Database;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations;

/// <inheritdoc />
[DbContext(typeof(AppDbContext))]
[Migration("20260904103000_RemoveLegacyHerbBaseProductAttributes")]
public partial class RemoveLegacyHerbBaseProductAttributes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM dbo.CrmBusinessEntityAttributes
            WHERE EntityType = 'CRM_HERB_BASE'
              AND AttributeCode IN ('CRM_MAIN_PRODUCT', 'MAIN_PRODUCT');
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
