using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AlignMallFrontendDataSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefaultFreightTemplateId",
                table: "MallStores",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VolumeCm3",
                table: "MallSkus",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "FreightTemplateId",
                table: "MallProducts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHot",
                table: "MallProducts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Sales",
                table: "MallProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "AddressId",
                table: "MallOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MemberId",
                table: "MallOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGift",
                table: "MallOrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalUnitPrice",
                table: "MallOrderItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PromotionId",
                table: "MallOrderItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromotionName",
                table: "MallOrderItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "MallCategories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "MallCategories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "MallCategories",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "MallAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiverName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReceiverPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProvinceCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ProvinceName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CityCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CityName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DistrictCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DistrictName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DetailAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallAddresses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallArticles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArticleType = table.Column<int>(type: "int", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SeoKeywords = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SeoDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsTop = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallArticles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallBanners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LinkType = table.Column<int>(type: "int", nullable: false),
                    LinkValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallBanners", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallCarts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastActiveAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallCarts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallCoupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CouponType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ThresholdAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxDiscount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    RemainCount = table.Column<int>(type: "int", nullable: false),
                    PerLimit = table.Column<int>(type: "int", nullable: false),
                    ValidDays = table.Column<int>(type: "int", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScopeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScopeValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallCoupons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallFreightTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BillingType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallFreightTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallFreightTemplates_MallStores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "MallStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MallMemberCoupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CouponId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CouponCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpireAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedOrderNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallMemberCoupons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallMemberLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LevelCode = table.Column<int>(type: "int", nullable: false),
                    DiscountRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MinSpent = table.Column<int>(type: "int", nullable: false),
                    MaxSpent = table.Column<int>(type: "int", nullable: false),
                    UpgradePoints = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallMemberLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallMemberPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeAmount = table.Column<int>(type: "int", nullable: false),
                    Balance = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RefNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallMemberPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Nickname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Avatar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Birthday = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    TotalPoints = table.Column<int>(type: "int", nullable: false),
                    AvailablePoints = table.Column<int>(type: "int", nullable: false),
                    TotalSpent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalOrders = table.Column<int>(type: "int", nullable: false),
                    LastConsumeAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    JoinAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallNotices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NoticeType = table.Column<int>(type: "int", nullable: false),
                    IsTop = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallNotices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PageCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PageName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeoTitle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SeoKeywords = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SeoDescription = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallPromotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TriggerConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ActionConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Combinable = table.Column<bool>(type: "bit", nullable: false),
                    MaxUsagePerUser = table.Column<int>(type: "int", nullable: false),
                    MaxUsagePerOrder = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallPromotions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallPromotionUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallPromotionUsages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReturnNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefundId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExpressCompany = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TrackingNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallReturns_MallOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "MallOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MallReturns_MallPayments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "MallPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MallCartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallCartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallCartItems_MallCarts_CartId",
                        column: x => x.CartId,
                        principalTable: "MallCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MallFreightRegions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProvinceCodes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CityCodes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FirstValue = table.Column<int>(type: "int", nullable: false),
                    FirstFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StepValue = table.Column<int>(type: "int", nullable: false),
                    StepFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FreeShippingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    FreeShippingQuantity = table.Column<int>(type: "int", nullable: true),
                    IsDefaultRegion = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallFreightRegions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallFreightRegions_MallFreightTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "MallFreightTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MallPromotionScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeTargetType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScopeTargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MallPromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallPromotionScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallPromotionScopes_MallPromotions_MallPromotionId",
                        column: x => x.MallPromotionId,
                        principalTable: "MallPromotions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MallStores_DefaultFreightTemplateId",
                table: "MallStores",
                column: "DefaultFreightTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MallProducts_FreightTemplateId",
                table: "MallProducts",
                column: "FreightTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MallOrders_AddressId",
                table: "MallOrders",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_MallOrders_MemberId",
                table: "MallOrders",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MallAddresses_UserId",
                table: "MallAddresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MallAddresses_UserId_IsDefault",
                table: "MallAddresses",
                columns: new[] { "UserId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_MallArticles_StoreId_ArticleType_IsActive_SortOrder",
                table: "MallArticles",
                columns: new[] { "StoreId", "ArticleType", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MallBanners_StoreId_Position_SortOrder",
                table: "MallBanners",
                columns: new[] { "StoreId", "Position", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallCartItems_CartId_SkuId",
                table: "MallCartItems",
                columns: new[] { "CartId", "SkuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallCarts_UserId_StoreId",
                table: "MallCarts",
                columns: new[] { "UserId", "StoreId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallCoupons_StoreId_Code",
                table: "MallCoupons",
                columns: new[] { "StoreId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallCoupons_StoreId_IsActive",
                table: "MallCoupons",
                columns: new[] { "StoreId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MallFreightRegions_TemplateId",
                table: "MallFreightRegions",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MallFreightTemplates_StoreId",
                table: "MallFreightTemplates",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_MallMemberCoupons_CouponCode",
                table: "MallMemberCoupons",
                column: "CouponCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallMemberCoupons_CouponId_MemberId",
                table: "MallMemberCoupons",
                columns: new[] { "CouponId", "MemberId" });

            migrationBuilder.CreateIndex(
                name: "IX_MallMemberCoupons_ExpireAt",
                table: "MallMemberCoupons",
                column: "ExpireAt");

            migrationBuilder.CreateIndex(
                name: "IX_MallMemberCoupons_MemberId_Status",
                table: "MallMemberCoupons",
                columns: new[] { "MemberId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MallMemberLevels_StoreId_IsActive",
                table: "MallMemberLevels",
                columns: new[] { "StoreId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MallMemberLevels_StoreId_LevelCode",
                table: "MallMemberLevels",
                columns: new[] { "StoreId", "LevelCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallMemberPoints_MemberId",
                table: "MallMemberPoints",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MallMemberPoints_MemberId_ChangeType",
                table: "MallMemberPoints",
                columns: new[] { "MemberId", "ChangeType" });

            migrationBuilder.CreateIndex(
                name: "IX_MallMembers_LevelId",
                table: "MallMembers",
                column: "LevelId");

            migrationBuilder.CreateIndex(
                name: "IX_MallMembers_Phone",
                table: "MallMembers",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallMembers_StoreId",
                table: "MallMembers",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_MallNotices_StoreId_IsTop_SortOrder",
                table: "MallNotices",
                columns: new[] { "StoreId", "IsTop", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MallPages_StoreId_PageCode",
                table: "MallPages",
                columns: new[] { "StoreId", "PageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallPromotions_IsActive",
                table: "MallPromotions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MallPromotionScopes_MallPromotionId",
                table: "MallPromotionScopes",
                column: "MallPromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_MallPromotionScopes_PromotionId",
                table: "MallPromotionScopes",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_MallPromotionUsages_PromotionId_UserId",
                table: "MallPromotionUsages",
                columns: new[] { "PromotionId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MallReturns_OrderId",
                table: "MallReturns",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MallReturns_PaymentId",
                table: "MallReturns",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_MallReturns_ReturnNo",
                table: "MallReturns",
                column: "ReturnNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_MallOrders_MallAddresses_AddressId",
                table: "MallOrders",
                column: "AddressId",
                principalTable: "MallAddresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MallOrders_MallMembers_MemberId",
                table: "MallOrders",
                column: "MemberId",
                principalTable: "MallMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MallProducts_MallFreightTemplates_FreightTemplateId",
                table: "MallProducts",
                column: "FreightTemplateId",
                principalTable: "MallFreightTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MallStores_MallFreightTemplates_DefaultFreightTemplateId",
                table: "MallStores",
                column: "DefaultFreightTemplateId",
                principalTable: "MallFreightTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MallOrders_MallAddresses_AddressId",
                table: "MallOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_MallOrders_MallMembers_MemberId",
                table: "MallOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_MallProducts_MallFreightTemplates_FreightTemplateId",
                table: "MallProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_MallStores_MallFreightTemplates_DefaultFreightTemplateId",
                table: "MallStores");

            migrationBuilder.DropTable(
                name: "MallAddresses");

            migrationBuilder.DropTable(
                name: "MallArticles");

            migrationBuilder.DropTable(
                name: "MallBanners");

            migrationBuilder.DropTable(
                name: "MallCartItems");

            migrationBuilder.DropTable(
                name: "MallCoupons");

            migrationBuilder.DropTable(
                name: "MallFreightRegions");

            migrationBuilder.DropTable(
                name: "MallMemberCoupons");

            migrationBuilder.DropTable(
                name: "MallMemberLevels");

            migrationBuilder.DropTable(
                name: "MallMemberPoints");

            migrationBuilder.DropTable(
                name: "MallMembers");

            migrationBuilder.DropTable(
                name: "MallNotices");

            migrationBuilder.DropTable(
                name: "MallPages");

            migrationBuilder.DropTable(
                name: "MallPromotionScopes");

            migrationBuilder.DropTable(
                name: "MallPromotionUsages");

            migrationBuilder.DropTable(
                name: "MallReturns");

            migrationBuilder.DropTable(
                name: "MallCarts");

            migrationBuilder.DropTable(
                name: "MallFreightTemplates");

            migrationBuilder.DropTable(
                name: "MallPromotions");

            migrationBuilder.DropIndex(
                name: "IX_MallStores_DefaultFreightTemplateId",
                table: "MallStores");

            migrationBuilder.DropIndex(
                name: "IX_MallProducts_FreightTemplateId",
                table: "MallProducts");

            migrationBuilder.DropIndex(
                name: "IX_MallOrders_AddressId",
                table: "MallOrders");

            migrationBuilder.DropIndex(
                name: "IX_MallOrders_MemberId",
                table: "MallOrders");

            migrationBuilder.DropColumn(
                name: "DefaultFreightTemplateId",
                table: "MallStores");

            migrationBuilder.DropColumn(
                name: "VolumeCm3",
                table: "MallSkus");

            migrationBuilder.DropColumn(
                name: "FreightTemplateId",
                table: "MallProducts");

            migrationBuilder.DropColumn(
                name: "IsHot",
                table: "MallProducts");

            migrationBuilder.DropColumn(
                name: "Sales",
                table: "MallProducts");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "MallOrders");

            migrationBuilder.DropColumn(
                name: "MemberId",
                table: "MallOrders");

            migrationBuilder.DropColumn(
                name: "IsGift",
                table: "MallOrderItems");

            migrationBuilder.DropColumn(
                name: "OriginalUnitPrice",
                table: "MallOrderItems");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "MallOrderItems");

            migrationBuilder.DropColumn(
                name: "PromotionName",
                table: "MallOrderItems");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "MallCategories");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "MallCategories");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "MallCategories");

        }
    }
}
