-- ============================================
-- 百度地图 POI 中草药种植基地数据表 (SQL Server)
-- 用于保存百度 Place API v3 区域检索返回结果
-- ============================================

IF OBJECT_ID(N'dbo.baidu_poi_herb_base', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.baidu_poi_herb_base (
        [id]                BIGINT              IDENTITY(1,1)   NOT NULL,   -- 主键
        [uid]               NVARCHAR(64)        NOT NULL,                   -- 百度POI唯一标识
        [name]              NVARCHAR(255)       NOT NULL DEFAULT '',        -- POI名称
        [lat]               DECIMAL(10,6)       NULL,                       -- 纬度(百度坐标系bd09ll)
        [lng]               DECIMAL(10,6)       NULL,                       -- 经度(百度坐标系bd09ll)
        [province]          NVARCHAR(50)        NOT NULL DEFAULT '',        -- 所属省份
        [city]              NVARCHAR(50)        NOT NULL DEFAULT '',        -- 所属城市
        [area]              NVARCHAR(50)        NOT NULL DEFAULT '',        -- 所属区县
        [town]              NVARCHAR(50)        NOT NULL DEFAULT '',        -- 乡镇/街道
        [town_code]         INT                 NULL,                       -- 乡镇编码
        [adcode]            INT                 NULL,                       -- 行政区划代码
        [address]           NVARCHAR(500)       NOT NULL DEFAULT '',        -- 详细地址
        [telephone]         NVARCHAR(100)       NOT NULL DEFAULT '',        -- 电话
        [street_id]         NVARCHAR(64)        NOT NULL DEFAULT '',        -- 街景图ID
        [detail]            TINYINT             NOT NULL DEFAULT 0,         -- 是否有详情页(1有0无)
        [detail_info]       NVARCHAR(MAX)       NULL,                       -- 扩展信息JSON(scope=2时返回)
        [search_keyword]    NVARCHAR(100)       NOT NULL DEFAULT '',        -- 搜索关键词
        [api_status]        INT                 NOT NULL DEFAULT 0,         -- API返回状态码
        [api_response]      NVARCHAR(MAX)       NULL,                       -- API原始响应JSON(备用)
        [created_at]        DATETIME2           NOT NULL DEFAULT GETDATE(), -- 创建时间
        [updated_at]        DATETIME2           NOT NULL DEFAULT GETDATE(), -- 更新时间

        CONSTRAINT [PK_baidu_poi_herb_base] PRIMARY KEY CLUSTERED ([id]),
        CONSTRAINT [UQ_baidu_poi_herb_base_uid] UNIQUE ([uid])
    );

    -- 索引
    CREATE NONCLUSTERED INDEX [IX_herb_name]         ON dbo.baidu_poi_herb_base ([name]);
    CREATE NONCLUSTERED INDEX [IX_herb_province]     ON dbo.baidu_poi_herb_base ([province]);
    CREATE NONCLUSTERED INDEX [IX_herb_city]         ON dbo.baidu_poi_herb_base ([city]);
    CREATE NONCLUSTERED INDEX [IX_herb_adcode]       ON dbo.baidu_poi_herb_base ([adcode]);
    CREATE NONCLUSTERED INDEX [IX_herb_lat_lng]      ON dbo.baidu_poi_herb_base ([lat], [lng]);
    CREATE NONCLUSTERED INDEX [IX_herb_keyword]      ON dbo.baidu_poi_herb_base ([search_keyword]);
    CREATE NONCLUSTERED INDEX [IX_herb_created_at]   ON dbo.baidu_poi_herb_base ([created_at]);

    -- 扩展属性说明
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'百度地图中草药种植基地POI数据', @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'baidu_poi_herb_base';
END;
GO


-- ============================================
-- updated_at 自动更新触发器
-- ============================================

IF OBJECT_ID(N'dbo.trg_herb_updated_at', N'TR') IS NOT NULL
    DROP TRIGGER dbo.trg_herb_updated_at;
GO

CREATE TRIGGER dbo.trg_herb_updated_at
ON dbo.baidu_poi_herb_base
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE t
    SET [updated_at] = GETDATE()
    FROM dbo.baidu_poi_herb_base t
    INNER JOIN inserted i ON t.[id] = i.[id];
END;
GO


-- ============================================
-- 插入/更新存储过程 (MERGE 防重复)
-- ============================================

IF OBJECT_ID(N'dbo.usp_herb_upsert', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_herb_upsert;
GO

CREATE PROCEDURE dbo.usp_herb_upsert
    @uid                NVARCHAR(64),
    @name               NVARCHAR(255),
    @lat                DECIMAL(10,6),
    @lng                DECIMAL(10,6),
    @province           NVARCHAR(50),
    @city               NVARCHAR(50),
    @area               NVARCHAR(50),
    @town               NVARCHAR(50),
    @town_code          INT,
    @adcode             INT,
    @address            NVARCHAR(500),
    @telephone          NVARCHAR(100),
    @street_id          NVARCHAR(64),
    @detail             TINYINT,
    @detail_info        NVARCHAR(MAX),
    @search_keyword     NVARCHAR(100),
    @api_status         INT,
    @api_response       NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    MERGE dbo.baidu_poi_herb_base AS t
    USING (SELECT
        @uid            AS [uid],
        @name           AS [name],
        @lat            AS [lat],
        @lng            AS [lng],
        @province       AS [province],
        @city           AS [city],
        @area           AS [area],
        @town           AS [town],
        @town_code      AS [town_code],
        @adcode         AS [adcode],
        @address        AS [address],
        @telephone      AS [telephone],
        @street_id      AS [street_id],
        @detail         AS [detail],
        @detail_info    AS [detail_info],
        @search_keyword AS [search_keyword],
        @api_status     AS [api_status],
        @api_response   AS [api_response]
    ) AS s ON t.[uid] = s.[uid]
    WHEN MATCHED THEN UPDATE SET
        [name]              = s.[name],
        [lat]               = s.[lat],
        [lng]               = s.[lng],
        [province]          = s.[province],
        [city]              = s.[city],
        [area]              = s.[area],
        [town]              = s.[town],
        [town_code]         = s.[town_code],
        [adcode]             = s.[adcode],
        [address]           = s.[address],
        [telephone]         = s.[telephone],
        [street_id]         = s.[street_id],
        [detail]            = s.[detail],
        [detail_info]       = s.[detail_info],
        [search_keyword]    = s.[search_keyword],
        [api_status]        = s.[api_status],
        [api_response]      = s.[api_response]
    WHEN NOT MATCHED THEN INSERT (
        [uid], [name], [lat], [lng], [province], [city], [area], [town],
        [town_code], [adcode], [address], [telephone], [street_id],
        [detail], [detail_info], [search_keyword], [api_status], [api_response]
    ) VALUES (
        s.[uid], s.[name], s.[lat], s.[lng], s.[province], s.[city], s.[area], s.[town],
        s.[town_code], s.[adcode], s.[address], s.[telephone], s.[street_id],
        s.[detail], s.[detail_info], s.[search_keyword], s.[api_status], s.[api_response]
    );
END;
GO
