using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QPS.Infrastructure.Database;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260831140000_AddCrmHerbProductDictionary")]
public partial class AddCrmHerbProductDictionary : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF NOT EXISTS (SELECT 1 FROM [SystemDataDictionaries] WHERE [Code] = 'CRM_HERB_PRODUCT' AND [IsDeleted] = 0)
            BEGIN
                INSERT INTO [SystemDataDictionaries] ([Id], [ParentId], [Code], [Name], [Value], [Description], [SortOrder], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted])
                VALUES (NEWID(), NULL, 'CRM_HERB_PRODUCT', N'中药材品类', N'中药材品类', N'CRM 品类统一维护根节点', 0, 1, GETDATE(), 'migration', GETDATE(), 'migration', 0);
            END;

            INSERT INTO [SystemDataDictionaries] ([Id], [ParentId], [Code], [Name], [Value], [Description], [SortOrder], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted])
            SELECT NEWID(), [root].[Id], [source].[ProductName], [source].[ProductName], [source].[ProductName], N'历史品类回填', 0, 1, GETDATE(), 'migration', GETDATE(), 'migration', 0
            FROM (
                SELECT DISTINCT LTRIM(RTRIM([AttributeValue])) AS [ProductName]
                FROM [CrmBusinessEntityAttributes]
                WHERE [IsDeleted] = 0
                  AND [AttributeCode] IN ('CRM_MAIN_PRODUCT', 'PURCHASE_PRODUCT')
                  AND LTRIM(RTRIM([AttributeValue])) <> ''
            ) AS [source]
            CROSS JOIN [SystemDataDictionaries] AS [root]
            WHERE [root].[Code] = 'CRM_HERB_PRODUCT'
              AND [root].[IsDeleted] = 0
              AND NOT EXISTS (
                  SELECT 1
                  FROM [SystemDataDictionaries] AS [item]
                  WHERE [item].[ParentId] = [root].[Id]
                    AND [item].[Code] = [source].[ProductName]
                    AND [item].[IsDeleted] = 0
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE [item]
            FROM [SystemDataDictionaries] AS [item]
            JOIN [SystemDataDictionaries] AS [root] ON [root].[Id] = [item].[ParentId]
            WHERE [root].[Code] = 'CRM_HERB_PRODUCT'
              AND [item].[CreatedBy] = 'migration'
              AND NOT EXISTS (
                  SELECT 1 FROM [CrmBusinessEntityAttributes] AS [attribute]
                  WHERE [attribute].[IsDeleted] = 0 AND [attribute].[AttributeValue] = [item].[Name]
              );

            DELETE [root]
            FROM [SystemDataDictionaries] AS [root]
            WHERE [root].[Code] = 'CRM_HERB_PRODUCT'
              AND [root].[CreatedBy] = 'migration'
              AND NOT EXISTS (SELECT 1 FROM [SystemDataDictionaries] AS [child] WHERE [child].[ParentId] = [root].[Id]);
            """);
    }
}
