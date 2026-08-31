using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QPS.Infrastructure.Database;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260831150000_NormalizeCrmHerbProductDictionary")]
public partial class NormalizeCrmHerbProductDictionary : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @rootId uniqueidentifier = (
                SELECT TOP 1 [Id]
                FROM [SystemDataDictionaries]
                WHERE [Code] = 'CRM_HERB_PRODUCT' AND [IsDeleted] = 0
            );

            UPDATE [CrmBusinessEntityAttributes]
            SET [AttributeValue] = N'天麻'
            WHERE [IsDeleted] = 0
              AND [AttributeCode] IN ('CRM_MAIN_PRODUCT', 'PURCHASE_PRODUCT')
              AND [AttributeValue] = 'TIAN_MA';

            UPDATE [SystemDataDictionaries]
            SET [IsDeleted] = 1, [Description] = N'已归并至天麻'
            WHERE [ParentId] = @rootId
              AND [IsDeleted] = 0
              AND [Code] = 'TIAN_MA';

            UPDATE [SystemDataDictionaries]
            SET [IsActive] = 0, [Description] = N'历史占位值，仅保留历史记录，不可选择'
            WHERE [ParentId] = @rootId
              AND [IsDeleted] = 0
              AND [Name] IN (N'???', N'0');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
