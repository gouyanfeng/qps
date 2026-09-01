using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QPS.Infrastructure.Database;

#nullable disable

namespace QPS.Infrastructure.Database.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260901090000_NormalizeCrmHerbBaseProductAttributes")]
public partial class NormalizeCrmHerbBaseProductAttributes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @rootId uniqueidentifier = (
                SELECT TOP 1 [Id]
                FROM [SystemDataDictionaries]
                WHERE [Code] = 'CRM_HERB_PRODUCT' AND [IsDeleted] = 0
            );

            DECLARE @targets TABLE ([Name] nvarchar(100));
            INSERT INTO @targets VALUES
                (N'艾草'), (N'黄芪'), (N'款冬花'), (N'党参'), (N'红花'), (N'黄精'), (N'玉竹'), (N'铁皮石斛'),
                (N'芍药'), (N'柴胡'), (N'刺五加'), (N'淫羊藿'), (N'苦参'), (N'人参'), (N'枳壳'), (N'板蓝根'),
                (N'赤芍'), (N'灯盏细辛'), (N'核桃楸'), (N'莲子'), (N'灵芝'), (N'升麻'), (N'五味子'),
                (N'紫皮石斛'), (N'黄芩');

            INSERT INTO [SystemDataDictionaries] ([Id], [ParentId], [Code], [Name], [Value], [Description], [SortOrder], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted])
            SELECT NEWID(), @rootId, [target].[Name], [target].[Name], [target].[Name], N'由历史基地品类提炼补充', 0, 1, GETDATE(), 'migration', GETDATE(), 'migration', 0
            FROM @targets AS [target]
            WHERE NOT EXISTS (
                SELECT 1
                FROM [SystemDataDictionaries] AS [item]
                WHERE [item].[ParentId] = @rootId AND [item].[Name] = [target].[Name] AND [item].[IsDeleted] = 0
            );

            DECLARE @split TABLE ([Source] nvarchar(100), [Target] nvarchar(100));
            INSERT INTO @split VALUES
                (N'艾草、黄芪、款冬花、党参', N'艾草'),
                (N'艾草、黄芪、款冬花、党参', N'黄芪'),
                (N'艾草、黄芪、款冬花、党参', N'款冬花'),
                (N'艾草、黄芪、款冬花、党参', N'党参'),
                (N'红花/黄精', N'红花'),
                (N'红花/黄精', N'黄精'),
                (N'玉竹、黄精、铁皮石斛', N'玉竹'),
                (N'玉竹、黄精、铁皮石斛', N'黄精'),
                (N'玉竹、黄精、铁皮石斛', N'铁皮石斛');

            INSERT INTO [CrmBusinessEntityAttributes] ([Id], [EntityType], [EntityId], [AttributeCode], [AttributeValue], [SortOrder], [Remark], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted])
            SELECT NEWID(), [attribute].[EntityType], [attribute].[EntityId], [attribute].[AttributeCode], [split].[Target], [attribute].[SortOrder], [attribute].[Remark], GETDATE(), 'migration', GETDATE(), 'migration', 0
            FROM [CrmBusinessEntityAttributes] AS [attribute]
            JOIN @split AS [split] ON [split].[Source] = [attribute].[AttributeValue]
            WHERE [attribute].[IsDeleted] = 0
              AND [attribute].[EntityType] = 'CRM_HERB_BASE'
              AND [attribute].[AttributeCode] = 'CRM_MAIN_PRODUCT'
              AND NOT EXISTS (
                  SELECT 1
                  FROM [CrmBusinessEntityAttributes] AS [existing]
                  WHERE [existing].[IsDeleted] = 0
                    AND [existing].[EntityType] = [attribute].[EntityType]
                    AND [existing].[EntityId] = [attribute].[EntityId]
                    AND [existing].[AttributeCode] = [attribute].[AttributeCode]
                    AND [existing].[AttributeValue] = [split].[Target]
              );

            UPDATE [attribute]
            SET [IsDeleted] = 1
            FROM [CrmBusinessEntityAttributes] AS [attribute]
            WHERE [attribute].[IsDeleted] = 0
              AND [attribute].[EntityType] = 'CRM_HERB_BASE'
              AND [attribute].[AttributeCode] = 'CRM_MAIN_PRODUCT'
              AND [attribute].[AttributeValue] IN (SELECT [Source] FROM @split);

            DECLARE @replace TABLE ([Source] nvarchar(100), [Target] nvarchar(100));
            INSERT INTO @replace VALUES
                (N'黄精等', N'黄精'), (N'芍药等', N'芍药'), (N'柴胡其他中药材', N'柴胡'),
                (N'刺五加（林下）', N'刺五加'), (N'淫羊藿（箭叶淫羊藿）', N'淫羊藿'),
                (N'淫羊藿（柔毛淫羊藿）', N'淫羊藿'), (N'黄芪育苗', N'黄芪'), (N'黄精（多花黄精）', N'黄精'),
                (N'苦参（秋播）', N'苦参'), (N'林下参', N'人参'), (N'林下苦参', N'苦参'),
                (N'枳壳（枳实）', N'枳壳'), (N'板蓝根（大青叶）', N'板蓝根'), (N'柴胡（北柴胡）', N'柴胡'),
                (N'赤芍（秋播）', N'赤芍'), (N'灯盏细辛（灯盏花）', N'灯盏细辛'), (N'核桃楸（林下）', N'核桃楸'),
                (N'黄精（滇黄精）', N'黄精'), (N'黄芪（蒙古黄芪）', N'黄芪'), (N'黄芪（秋播）', N'黄芪'),
                (N'黄芩（秋播）', N'黄芩'), (N'莲子 （白莲子）', N'莲子'), (N'林下柴胡', N'柴胡'),
                (N'林下赤芍', N'赤芍'), (N'林下刺五加', N'刺五加'), (N'灵芝（赤芝）', N'灵芝'),
                (N'升麻（秋播）', N'升麻'), (N'五味子（秋播）', N'五味子'), (N'紫皮石斛（齿瓣石斛）', N'紫皮石斛');

            UPDATE [attribute]
            SET [AttributeValue] = [replace].[Target]
            FROM [CrmBusinessEntityAttributes] AS [attribute]
            JOIN @replace AS [replace] ON [replace].[Source] = [attribute].[AttributeValue]
            WHERE [attribute].[IsDeleted] = 0
              AND [attribute].[EntityType] = 'CRM_HERB_BASE'
              AND [attribute].[AttributeCode] = 'CRM_MAIN_PRODUCT';

            WITH [duplicates] AS (
                SELECT [Id], ROW_NUMBER() OVER (
                    PARTITION BY [EntityType], [EntityId], [AttributeCode], [AttributeValue]
                    ORDER BY [SortOrder], [CreatedAt], [Id]
                ) AS [RowNumber]
                FROM [CrmBusinessEntityAttributes]
                WHERE [IsDeleted] = 0
                  AND [EntityType] = 'CRM_HERB_BASE'
                  AND [AttributeCode] = 'CRM_MAIN_PRODUCT'
            )
            UPDATE [attribute]
            SET [IsDeleted] = 1
            FROM [CrmBusinessEntityAttributes] AS [attribute]
            JOIN [duplicates] ON [duplicates].[Id] = [attribute].[Id]
            WHERE [duplicates].[RowNumber] > 1;

            UPDATE [item]
            SET [IsActive] = 0, [Description] = N'已规范为标准品类，仅保留历史记录'
            FROM [SystemDataDictionaries] AS [item]
            WHERE [item].[ParentId] = @rootId
              AND [item].[IsDeleted] = 0
              AND [item].[Name] IN (
                  SELECT [Source] FROM @split
                  UNION
                  SELECT [Source] FROM @replace
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
