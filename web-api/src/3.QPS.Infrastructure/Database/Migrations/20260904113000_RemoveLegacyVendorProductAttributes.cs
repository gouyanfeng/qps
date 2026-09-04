using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QPS.Infrastructure.Database;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260904113000_RemoveLegacyVendorProductAttributes")]
public partial class RemoveLegacyVendorProductAttributes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM dbo.CrmBusinessEntityAttributes WHERE EntityType = 'CRM_VENDOR' AND AttributeCode = 'PURCHASE_PRODUCT';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
