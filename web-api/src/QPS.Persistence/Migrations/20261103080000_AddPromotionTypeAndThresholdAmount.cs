using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    public partial class AddPromotionTypeAndThresholdAmount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PromotionType",
                table: "MallPromotions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ThresholdAmount",
                table: "MallPromotions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            // 存量数据回填：PromotionType 从 ActionType 映射，ThresholdAmount 从 TriggerConfigJson 解析
            migrationBuilder.Sql(@"
                DECLARE @sql NVARCHAR(MAX) = N'
                DECLARE @id UNIQUEIDENTIFIER;
                DECLARE @actionType NVARCHAR(50);
                DECLARE @triggerConfig NVARCHAR(MAX);
                DECLARE @promotionType INT;
                DECLARE @thresholdAmount DECIMAL(18,2);
                DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
                    SELECT Id, ActionType, TriggerConfigJson FROM MallPromotions;
                OPEN cur;
                FETCH NEXT FROM cur INTO @id, @actionType, @triggerConfig;
                WHILE @@FETCH_STATUS = 0
                BEGIN
                    SET @promotionType = CASE @actionType
                        WHEN N''DiscountAmount'' THEN 0
                        WHEN N''DiscountRate'' THEN 1
                        WHEN N''Gift'' THEN 2
                        ELSE 0
                    END;
                    SET @thresholdAmount = 0;
                    IF @triggerConfig IS NOT NULL AND @triggerConfig LIKE N''%minAmount%''
                    BEGIN
                        DECLARE @ma NVARCHAR(50);
                        SET @ma = JSON_VALUE(@triggerConfig, N''$.minAmount'');
                        IF @ma IS NOT NULL TRY SET @thresholdAmount = CAST(@ma AS DECIMAL(18,2));
                    END
                    IF @thresholdAmount = 0 AND @triggerConfig IS NOT NULL AND @triggerConfig LIKE N''%minQuantity%''
                    BEGIN
                        DECLARE @mq NVARCHAR(50);
                        SET @mq = JSON_VALUE(@triggerConfig, N''$.minQuantity'');
                        IF @mq IS NOT NULL TRY SET @thresholdAmount = CAST(@mq AS DECIMAL(18,2));
                    END
                    UPDATE MallPromotions
                    SET PromotionType = @promotionType, ThresholdAmount = @thresholdAmount
                    WHERE Id = @id;
                    FETCH NEXT FROM cur INTO @id, @actionType, @triggerConfig;
                END
                CLOSE cur; DEALLOCATE cur;
                ';
                EXEC sp_executesql @sql;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThresholdAmount",
                table: "MallPromotions");

            migrationBuilder.DropColumn(
                name: "PromotionType",
                table: "MallPromotions");
        }
    }
}