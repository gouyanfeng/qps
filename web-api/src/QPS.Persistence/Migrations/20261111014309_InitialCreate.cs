using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QPS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CrmBusinessActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmBusinessActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrmBusinessEntityAttributes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AttributeValue = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmBusinessEntityAttributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrmContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Wechat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrmHerbBaseSubjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubjectType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Scale = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PrimaryContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrimaryContactPhone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastFollowAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFollowResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NextFollowAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmHerbBaseSubjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrmTransferRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "入库"),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromOwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ToOwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmTransferRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrmVendors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedVendorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PriorityLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LatestPurchaseTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LatestPurchaseDemandName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastFollowAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFollowResult = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NextFollowAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmVendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReceiverName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReceiverPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ProvinceName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CityName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
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
                name: "MallCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallCampaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallCartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "MallCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallCoupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                name: "MallProductReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MemberNickname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MemberLevelName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SpecText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Images = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false, defaultValue: ""),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReviewTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AuditStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ReplyContent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallProductReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallProductTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallProductTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallPromotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PromotionType = table.Column<int>(type: "int", nullable: false),
                    ThresholdAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ActionConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromotionTagId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                name: "MallPromotionScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeTargetType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScopeTargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallPromotionScopes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallStockLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkuCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SpecName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ChangeQuantity = table.Column<int>(type: "int", nullable: false),
                    BeforeStock = table.Column<int>(type: "int", nullable: false),
                    AfterStock = table.Column<int>(type: "int", nullable: false),
                    RelatedOrderNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallStockLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MallTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Product"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemChinaRegions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Level = table.Column<byte>(type: "tinyint", nullable: false),
                    ParentCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ProvinceCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CityCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemChinaRegions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemDataDictionaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemDataDictionaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemDataDictionaries_SystemDataDictionaries_ParentId",
                        column: x => x.ParentId,
                        principalTable: "SystemDataDictionaries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SystemErrorLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ErrorType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemOperationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OperatorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ChangeJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemOperationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PermissionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemRolePermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemRolePermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemUserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemUserRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RealName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CrmFollowRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FollowType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FollowResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntentLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NextFollowAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OperatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmFollowRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmFollowRecords_CrmContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "CrmContacts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CrmHerbBases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbBaseSubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BaseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SubjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Scale = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Area = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lat = table.Column<decimal>(type: "decimal(10,6)", precision: 10, scale: 6, nullable: true),
                    Lng = table.Column<decimal>(type: "decimal(10,6)", precision: 10, scale: 6, nullable: true),
                    CoordinateText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SourcePlatform = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryContactName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastFollowAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFollowResult = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NextFollowAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmHerbBases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmHerbBases_CrmHerbBaseSubjects_HerbBaseSubjectId",
                        column: x => x.HerbBaseSubjectId,
                        principalTable: "CrmHerbBaseSubjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrmVendorDemands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DemandNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DemandName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DemandAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExpectedDeliveryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReceivingAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClosedReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmVendorDemands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmVendorDemands_CrmContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "CrmContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmVendorDemands_CrmVendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "CrmVendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MallCampaignCoupons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CouponId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallCampaignCoupons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallCampaignCoupons_MallCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "MallCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MallCampaignCoupons_MallCoupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "MallCoupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MallFreightRegions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProvinceNames = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CityNames = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                name: "MallStores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DefaultFreightTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallStores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallStores_MallFreightTemplates_DefaultFreightTemplateId",
                        column: x => x.DefaultFreightTemplateId,
                        principalTable: "MallFreightTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MallOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReceiverName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReceiverPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReceiverAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    GoodsAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PromotionDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CouponDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CouponId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CouponName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FreightAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PayableAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalWeightGram = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    GiftShortageMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallOrders_MallMembers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "MallMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CrmHerbBaseSupplies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbBaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbBaseSubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuantityUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Specification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QualityRequirement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HarvestSeason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ExpectedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PriceUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SupplyCycle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmHerbBaseSupplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmHerbBaseSupplies_CrmHerbBases_HerbBaseId",
                        column: x => x.HerbBaseId,
                        principalTable: "CrmHerbBases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrmVendorDemandItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorDemandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuantityUnit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Specification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QualityRequirement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PriceUnit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedDeliveryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmVendorDemandItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmVendorDemandItems_CrmVendorDemands_VendorDemandId",
                        column: x => x.VendorDemandId,
                        principalTable: "CrmVendorDemands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MallProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MainImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AlbumImages = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetailHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsHot = table.Column<bool>(type: "bit", nullable: false),
                    Sales = table.Column<int>(type: "int", nullable: false),
                    FreightTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallProducts_MallCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "MallCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MallProducts_MallFreightTemplates_FreightTemplateId",
                        column: x => x.FreightTemplateId,
                        principalTable: "MallFreightTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MallProducts_MallStores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "MallStores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MallOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkuId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SkuCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SpecName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductImage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    WeightGram = table.Column<int>(type: "int", nullable: false),
                    ItemAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsGift = table.Column<bool>(type: "bit", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PromotionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OriginalUnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PromotionDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PromotionSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallOrderItems_MallOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "MallOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MallPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TransactionNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallPayments_MallOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "MallOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MallShipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpressCompany = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TrackingNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ShippedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsVoided = table.Column<bool>(type: "bit", nullable: false),
                    VoidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VoidedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VoidReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallShipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallShipments_MallOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "MallOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CrmMatchOpportunities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DemandItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DemandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VendorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DemandProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DemandQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuantityUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Specification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QualityRequirement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpectedDeliveryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveryAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NextAction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NextActionRemark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NextActionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CloseResult = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ClosedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CrmMatchOpportunities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmMatchOpportunities_CrmVendorDemandItems_DemandItemId",
                        column: x => x.DemandItemId,
                        principalTable: "CrmVendorDemandItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MallCampaignProducts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: ""),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallCampaignProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallCampaignProducts_MallCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "MallCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MallCampaignProducts_MallProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "MallProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MallSkus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkuCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SpecName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MarketPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    WeightGram = table.Column<int>(type: "int", nullable: false),
                    VolumeCm3 = table.Column<int>(type: "int", nullable: false),
                    Stock = table.Column<int>(type: "int", nullable: false),
                    WarnStock = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallSkus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallSkus_MallProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "MallProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MallRefunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefundNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EvidenceImages = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MallRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MallRefunds_MallOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "MallOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MallRefunds_MallPayments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "MallPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EvidenceImages = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RejectReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                name: "CrmMatchCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchOpportunityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbBaseSupplyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbBaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HerbBaseSubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HerbBaseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    QuantityUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Specification = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    QualityRequirement = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpectedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PriceUnit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SupplyCycle = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
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
                    table.PrimaryKey("PK_CrmMatchCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CrmMatchCandidates_CrmHerbBaseSupplies_HerbBaseSupplyId",
                        column: x => x.HerbBaseSupplyId,
                        principalTable: "CrmHerbBaseSupplies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CrmMatchCandidates_CrmMatchOpportunities_MatchOpportunityId",
                        column: x => x.MatchOpportunityId,
                        principalTable: "CrmMatchOpportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CrmBusinessActivities_EntityType_EntityId_CreatedAt",
                table: "CrmBusinessActivities",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmBusinessEntityAttributes_EntityType_EntityId_AttributeCode",
                table: "CrmBusinessEntityAttributes",
                columns: new[] { "EntityType", "EntityId", "AttributeCode" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmBusinessEntityAttributes_EntityType_EntityId_AttributeCode_AttributeValue",
                table: "CrmBusinessEntityAttributes",
                columns: new[] { "EntityType", "EntityId", "AttributeCode", "AttributeValue" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmContacts_EntityType_EntityId",
                table: "CrmContacts",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmContacts_EntityType_EntityId_Phone",
                table: "CrmContacts",
                columns: new[] { "EntityType", "EntityId", "Phone" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmFollowRecords_ContactId",
                table: "CrmFollowRecords",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmFollowRecords_EntityType_EntityId_CreatedAt",
                table: "CrmFollowRecords",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmHerbBases_HerbBaseSubjectId",
                table: "CrmHerbBases",
                column: "HerbBaseSubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmHerbBaseSubjects_SubjectName",
                table: "CrmHerbBaseSubjects",
                column: "SubjectName");

            migrationBuilder.CreateIndex(
                name: "IX_CrmHerbBaseSubjects_SubjectType_IsDeleted",
                table: "CrmHerbBaseSubjects",
                columns: new[] { "SubjectType", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmHerbBaseSupplies_HerbBaseId_Status",
                table: "CrmHerbBaseSupplies",
                columns: new[] { "HerbBaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmHerbBaseSupplies_HerbBaseSubjectId_Status",
                table: "CrmHerbBaseSupplies",
                columns: new[] { "HerbBaseSubjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmHerbBaseSupplies_ProductName_Status_ValidUntil",
                table: "CrmHerbBaseSupplies",
                columns: new[] { "ProductName", "Status", "ValidUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchCandidates_HerbBaseSupplyId",
                table: "CrmMatchCandidates",
                column: "HerbBaseSupplyId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchCandidates_MatchOpportunityId_HerbBaseSupplyId",
                table: "CrmMatchCandidates",
                columns: new[] { "MatchOpportunityId", "HerbBaseSupplyId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchOpportunities_DemandItemId",
                table: "CrmMatchOpportunities",
                column: "DemandItemId",
                unique: true,
                filter: "[Status] IN (N'推进中', N'暂停') AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CrmMatchOpportunities_OwnerUserId_NextActionAt",
                table: "CrmMatchOpportunities",
                columns: new[] { "OwnerUserId", "NextActionAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmTransferRecords_EntityType_EntityId_CreatedAt",
                table: "CrmTransferRecords",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendorDemandItems_VendorDemandId_SortOrder",
                table: "CrmVendorDemandItems",
                columns: new[] { "VendorDemandId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendorDemands_ContactId",
                table: "CrmVendorDemands",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendorDemands_DemandNo",
                table: "CrmVendorDemands",
                column: "DemandNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendorDemands_VendorId_Status_DemandAt",
                table: "CrmVendorDemands",
                columns: new[] { "VendorId", "Status", "DemandAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendors_LatestPurchaseTime",
                table: "CrmVendors",
                column: "LatestPurchaseTime");

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendors_NormalizedVendorName",
                table: "CrmVendors",
                column: "NormalizedVendorName",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendors_OwnerUserId_NextFollowAt",
                table: "CrmVendors",
                columns: new[] { "OwnerUserId", "NextFollowAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CrmVendors_PriorityLevel",
                table: "CrmVendors",
                column: "PriorityLevel");

            migrationBuilder.CreateIndex(
                name: "IX_MallAddresses_UserId",
                table: "MallAddresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MallAddresses_UserId_IsDefault",
                table: "MallAddresses",
                columns: new[] { "UserId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_MallArticles_ArticleType_IsActive_SortOrder",
                table: "MallArticles",
                columns: new[] { "ArticleType", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MallBanners_Position_SortOrder",
                table: "MallBanners",
                columns: new[] { "Position", "SortOrder" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallCampaignCoupons_CampaignId_CouponId",
                table: "MallCampaignCoupons",
                columns: new[] { "CampaignId", "CouponId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallCampaignCoupons_CampaignId_SortOrder",
                table: "MallCampaignCoupons",
                columns: new[] { "CampaignId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MallCampaignCoupons_CouponId",
                table: "MallCampaignCoupons",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_MallCampaignProducts_CampaignId_ProductId",
                table: "MallCampaignProducts",
                columns: new[] { "CampaignId", "ProductId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallCampaignProducts_CampaignId_SortOrder",
                table: "MallCampaignProducts",
                columns: new[] { "CampaignId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MallCampaignProducts_ProductId",
                table: "MallCampaignProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MallCampaigns_IsActive_SortOrder",
                table: "MallCampaigns",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MallCartItems_UserId_SkuId",
                table: "MallCartItems",
                columns: new[] { "UserId", "SkuId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallCategories_Code",
                table: "MallCategories",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallCoupons_Code",
                table: "MallCoupons",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallCoupons_IsActive",
                table: "MallCoupons",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MallFreightRegions_TemplateId",
                table: "MallFreightRegions",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MallMemberCoupons_CouponCode",
                table: "MallMemberCoupons",
                column: "CouponCode",
                unique: true,
                filter: "[IsDeleted] = 0");

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
                name: "IX_MallMemberLevels_IsActive",
                table: "MallMemberLevels",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MallMemberLevels_LevelCode",
                table: "MallMemberLevels",
                column: "LevelCode",
                unique: true,
                filter: "[IsDeleted] = 0");

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
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallNotices_IsTop_SortOrder",
                table: "MallNotices",
                columns: new[] { "IsTop", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_MallOrderItems_OrderId",
                table: "MallOrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MallOrders_MemberId",
                table: "MallOrders",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_MallOrders_OrderNo",
                table: "MallOrders",
                column: "OrderNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallOrders_Status",
                table: "MallOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MallPages_PageCode",
                table: "MallPages",
                column: "PageCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallPayments_OrderId",
                table: "MallPayments",
                column: "OrderId",
                unique: true,
                filter: "[Status] = 'Success' AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallPayments_PaymentNo",
                table: "MallPayments",
                column: "PaymentNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallProductReviews_OrderItemId",
                table: "MallProductReviews",
                column: "OrderItemId",
                unique: true,
                filter: "[OrderItemId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallProductReviews_ProductId_ReviewTime",
                table: "MallProductReviews",
                columns: new[] { "ProductId", "ReviewTime" });

            migrationBuilder.CreateIndex(
                name: "IX_MallProducts_CategoryId",
                table: "MallProducts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MallProducts_Code",
                table: "MallProducts",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallProducts_FreightTemplateId",
                table: "MallProducts",
                column: "FreightTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MallProducts_StoreId",
                table: "MallProducts",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_MallProductTags_ProductsId_TagsId",
                table: "MallProductTags",
                columns: new[] { "ProductsId", "TagsId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallPromotions_IsActive",
                table: "MallPromotions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MallPromotionScopes_PromotionId",
                table: "MallPromotionScopes",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_MallRefunds_OrderId",
                table: "MallRefunds",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MallRefunds_PaymentId",
                table: "MallRefunds",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_MallRefunds_RefundNo",
                table: "MallRefunds",
                column: "RefundNo",
                unique: true,
                filter: "[IsDeleted] = 0");

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

            migrationBuilder.CreateIndex(
                name: "IX_MallShipments_OrderId",
                table: "MallShipments",
                column: "OrderId",
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsVoided] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallShipments_ShipmentNo",
                table: "MallShipments",
                column: "ShipmentNo",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallShipments_TrackingNo",
                table: "MallShipments",
                column: "TrackingNo",
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsVoided] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallSkus_ProductId",
                table: "MallSkus",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_MallSkus_SkuCode",
                table: "MallSkus",
                column: "SkuCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallStockLogs_ChangeType_CreatedAt",
                table: "MallStockLogs",
                columns: new[] { "ChangeType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MallStockLogs_SkuId",
                table: "MallStockLogs",
                column: "SkuId");

            migrationBuilder.CreateIndex(
                name: "IX_MallStores_Code",
                table: "MallStores",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallStores_DefaultFreightTemplateId",
                table: "MallStores",
                column: "DefaultFreightTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MallTags_Type_Code",
                table: "MallTags",
                columns: new[] { "Type", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MallTags_Type_Name",
                table: "MallTags",
                columns: new[] { "Type", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SystemChinaRegions_Code",
                table: "SystemChinaRegions",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_SystemChinaRegions_Level_ParentCode",
                table: "SystemChinaRegions",
                columns: new[] { "Level", "ParentCode" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemDataDictionaries_ParentId",
                table: "SystemDataDictionaries",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemOperationLogs_ActionType",
                table: "SystemOperationLogs",
                column: "ActionType");

            migrationBuilder.CreateIndex(
                name: "IX_SystemOperationLogs_CreatedAt",
                table: "SystemOperationLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemOperationLogs_EntityType_EntityId",
                table: "SystemOperationLogs",
                columns: new[] { "EntityType", "EntityId" });

            // System 基线种子数据：角色、用户、权限、角色-权限、字典、中国地区（导出自 QPS_CRM）
            migrationBuilder.Sql("""
/* System baseline data exported from QPS_CRM on 2026-11-11 */
SET NOCOUNT ON;

-- SystemRoles (2 rows)
INSERT INTO [dbo].[SystemRoles] ([Id], [Name], [Code], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted]) VALUES
('46C37FC5-1BCF-4747-A71C-2F9041243CD3', '普通用户', 'user', '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('A6291778-0A2F-4235-BC36-5675D897ABAF', '系统管理员', 'admin', '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0);

-- SystemUsers (2 rows)
INSERT INTO [dbo].[SystemUsers] ([Id], [RoleId], [Username], [PasswordHash], [RealName], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted]) VALUES
('6F913E3C-0693-47C8-B858-99235A79DCE5', '46C37FC5-1BCF-4747-A71C-2F9041243CD3', 'user', '123456', '普通用户', 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('7BC372F3-C80F-40FD-9019-F9C8D67343FF', 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'admin', '123456', '系统管理员', 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0);

-- SystemUserRoles (2 rows)
INSERT INTO [dbo].[SystemUserRoles] ([Id], [UserId], [RoleId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted]) VALUES
('541C0A13-E126-4377-8134-3E630F6D1857', '6F913E3C-0693-47C8-B858-99235A79DCE5', '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('3C039871-FC4F-439B-B235-6B0ACDED4E54', '7BC372F3-C80F-40FD-9019-F9C8D67343FF', 'A6291778-0A2F-4235-BC36-5675D897ABAF', '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0);

-- SystemPermissions (88 rows) 按菜单 authMenuList.json 重建
INSERT INTO [dbo].[SystemPermissions] ([Id], [Name], [Code], [ParentId], [PermissionType], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted]) VALUES
('3C70E31D-CAB2-5B58-B380-59B40B421CDC', N'系统权限', 'ROOT', NULL, N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('D3D4D9F8-C36B-58C1-BC49-A7278A2A6C7C', N'首页', 'HOME', '3C70E31D-CAB2-5B58-B380-59B40B421CDC', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('3EF2F682-24F6-596F-935D-9E5F0C9D9705', N'CRM管理', 'CRM', '3C70E31D-CAB2-5B58-B380-59B40B421CDC', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('E1D5A232-0D1A-53D3-A62B-CD63A7357A83', N'我的待办', 'CRM_FOLLOW_TASK', '3EF2F682-24F6-596F-935D-9E5F0C9D9705', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('D72D8995-98B3-5451-9543-FB864A335589', N'基地管理', 'CRM_HERB_BASE', '3EF2F682-24F6-596F-935D-9E5F0C9D9705', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('F456E35C-33CF-57CC-81C7-7F64AC416081', N'供应列表', 'CRM_HERB_BASE_SUPPLY', '3EF2F682-24F6-596F-935D-9E5F0C9D9705', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('C5041A6A-65A5-5699-B744-0B6921C011D8', N'厂商管理', 'CRM_VENDOR', '3EF2F682-24F6-596F-935D-9E5F0C9D9705', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('A8623F7A-A38A-52F8-A14B-B4F9BC7C86E5', N'采购需求', 'CRM_PURCHASE_DEMAND_VIEW', '3EF2F682-24F6-596F-935D-9E5F0C9D9705', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('08DCE4B9-8F1C-56E2-955E-A715C2BCA5AB', N'撮合机会', 'CRM_MATCH_OPPORTUNITY_VIEW', '3EF2F682-24F6-596F-935D-9E5F0C9D9705', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('5B2C3350-CEED-5AB7-B870-D2BEB15DB7D2', N'新增基地', 'CRM_HERB_BASE_ADD', 'D72D8995-98B3-5451-9543-FB864A335589', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('FAE40574-8112-58D8-B23B-82D0D220AEAA', N'编辑基地', 'CRM_HERB_BASE_EDIT', 'D72D8995-98B3-5451-9543-FB864A335589', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('0DB7C19E-D26D-5F75-8240-C15E52B57929', N'删除基地', 'CRM_HERB_BASE_DELETE', 'D72D8995-98B3-5451-9543-FB864A335589', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('3DD66318-153A-5985-BFBC-560C5950A7AB', N'变更基地状态', 'CRM_HERB_BASE_STATUS', 'D72D8995-98B3-5451-9543-FB864A335589', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('C08A6930-E16A-54AE-8672-CE867559D624', N'新增基地联系人', 'CRM_HERB_BASE_CONTACT_ADD', 'D72D8995-98B3-5451-9543-FB864A335589', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('71A18294-EBC8-55AA-A49A-752EFE8EC220', N'编辑基地联系人', 'CRM_HERB_BASE_CONTACT_EDIT', 'D72D8995-98B3-5451-9543-FB864A335589', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('775B5324-252F-5262-88BC-00C93D27EEA8', N'设为首要联系人', 'CRM_HERB_BASE_CONTACT_PRIMARY', 'D72D8995-98B3-5451-9543-FB864A335589', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('E5962908-7B07-5DC9-A983-30A706BDAD32', N'维护供应信息', 'CRM_HERB_BASE_SUPPLY_MANAGE', 'F456E35C-33CF-57CC-81C7-7F64AC416081', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('7FE63AA3-3D2D-5EBB-B668-6DAB925D2506', N'记录跟进', 'CRM_FOLLOW', 'E1D5A232-0D1A-53D3-A62B-CD63A7357A83', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('DB439BA0-6985-56DC-9523-21BD5061767D', N'CRM流转', 'CRM_TRANSFER', 'D72D8995-98B3-5451-9543-FB864A335589', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('F6E7F82C-0A0F-52EB-93D7-A432183566BA', N'新增厂商', 'CRM_VENDOR_ADD', 'C5041A6A-65A5-5699-B744-0B6921C011D8', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('5EFA101B-2C7B-5682-82E4-279C5EFEEAF5', N'编辑厂商', 'CRM_VENDOR_EDIT', 'C5041A6A-65A5-5699-B744-0B6921C011D8', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('B935D9CC-D9AE-5605-9F90-7562992792BC', N'维护采购需求', 'CRM_PURCHASE_DEMAND_MANAGE', 'A8623F7A-A38A-52F8-A14B-B4F9BC7C86E5', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('8DDF8455-8410-5AFC-896E-1FEE8DB8FFD4', N'维护撮合机会', 'CRM_MATCH_OPPORTUNITY_MANAGE', '08DCE4B9-8F1C-56E2-955E-A715C2BCA5AB', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'商城管理', 'MALL', '3C70E31D-CAB2-5B58-B380-59B40B421CDC', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('A73C26FC-02C7-59C3-9598-5ADDDD006236', N'商品管理', 'MALL_PRODUCT', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('183B65E4-74E1-57B7-AF30-9C59D261905B', N'商品分类', 'MALL_CATEGORY', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('20E7DFC8-EEEF-5E3C-98A7-8A7316C77A08', N'标签管理', 'MALL_TAG', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('9DBC0016-B674-55A9-A150-AA1E34A53319', N'评价管理', 'MALL_REVIEW', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('8A433F7E-8FDC-5B18-AA99-15E555A1C129', N'库存管理', 'MALL_STOCK', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('140CEDD0-5C87-5F76-92F9-48FE5B8EFA87', N'促销活动', 'MALL_PROMOTION', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('5FDB6F28-10E5-55FD-8B8F-B47655368BC8', N'专题活动', 'MALL_CAMPAIGN', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('E964B0FE-48A6-51C9-8E79-236966FDB010', N'优惠券管理', 'MALL_COUPON', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('E3286CAC-A365-56A9-BF2D-4EE6BFCB00CE', N'Banner管理', 'MALL_BANNER', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('A6920F3A-9FE8-571C-ADE8-ED364DEBE71F', N'订单管理', 'MALL_ORDER', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('5C511A49-AA96-585B-9810-DF7C436B83FC', N'发货管理', 'MALL_SHIPMENT', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('4CF764F7-D12F-52B5-A3AB-0CBC65EC8A5A', N'退货管理', 'MALL_RETURN_ORDER', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('3393426C-7901-5991-8286-EF93E2BC6DF9', N'商城门店', 'MALL_STORE', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('C1F11E0D-C9B2-52A4-95C8-A486F831FD26', N'运费规则', 'MALL_FREIGHT', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('A76BEDC8-6A08-557A-90DC-2D628BE83377', N'新增商品', 'MALL_PRODUCT_ADD', 'A73C26FC-02C7-59C3-9598-5ADDDD006236', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('127D7E3E-F53E-5ADD-B869-561D4C168F29', N'编辑商品', 'MALL_PRODUCT_EDIT', 'A73C26FC-02C7-59C3-9598-5ADDDD006236', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('E7BE1B6D-E675-5013-8203-417C2DEE49D6', N'删除商品', 'MALL_PRODUCT_DELETE', 'A73C26FC-02C7-59C3-9598-5ADDDD006236', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('A526DB8D-64D8-5AE6-B92C-DD94694F6C48', N'新增商品分类', 'MALL_CATEGORY_ADD', '183B65E4-74E1-57B7-AF30-9C59D261905B', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('B9BFB39C-E33D-55C6-8447-D4EEF4026D0E', N'编辑商品分类', 'MALL_CATEGORY_EDIT', '183B65E4-74E1-57B7-AF30-9C59D261905B', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('506F05F4-E874-5989-AE9B-EA7FDB322BD0', N'删除商品分类', 'MALL_CATEGORY_DELETE', '183B65E4-74E1-57B7-AF30-9C59D261905B', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('F578ED5F-8DF2-5C79-A7A9-BA00DB09B052', N'新增标签', 'MALL_TAG_ADD', '20E7DFC8-EEEF-5E3C-98A7-8A7316C77A08', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('3DC70916-5B6E-5D7A-9AAE-B06B257F3ADE', N'编辑标签', 'MALL_TAG_EDIT', '20E7DFC8-EEEF-5E3C-98A7-8A7316C77A08', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('8CA7B98F-0308-5BCD-8CF1-3981B6C54256', N'删除标签', 'MALL_TAG_DELETE', '20E7DFC8-EEEF-5E3C-98A7-8A7316C77A08', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('633FED4D-D577-567B-98E2-D56DFB1F2BFE', N'审核/回复评价', 'MALL_REVIEW_EDIT', '9DBC0016-B674-55A9-A150-AA1E34A53319', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('BB302D03-B0CE-56D3-97B2-A949EACB0705', N'删除评价', 'MALL_REVIEW_DELETE', '9DBC0016-B674-55A9-A150-AA1E34A53319', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('F0CD7FBC-7B8F-579C-BB91-D086F36788E1', N'调整库存', 'MALL_STOCK_EDIT', '8A433F7E-8FDC-5B18-AA99-15E555A1C129', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0);
INSERT INTO [dbo].[SystemPermissions] ([Id], [Name], [Code], [ParentId], [PermissionType], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted]) VALUES
('6C747389-B4E3-5169-A2D5-3A0807008266', N'新增促销活动', 'MALL_PROMOTION_ADD', '140CEDD0-5C87-5F76-92F9-48FE5B8EFA87', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('83CE5522-6C1B-5479-8DA6-AA98593B7599', N'编辑促销活动', 'MALL_PROMOTION_EDIT', '140CEDD0-5C87-5F76-92F9-48FE5B8EFA87', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('15B779FF-63AD-5A93-860C-6D5B68B78A6E', N'删除促销活动', 'MALL_PROMOTION_DELETE', '140CEDD0-5C87-5F76-92F9-48FE5B8EFA87', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('BAECB234-2285-5D01-A73E-579FBCB299EC', N'新增专题活动', 'MALL_CAMPAIGN_ADD', '5FDB6F28-10E5-55FD-8B8F-B47655368BC8', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('3BE8D281-033D-5729-9F83-048BBB149436', N'编辑专题活动', 'MALL_CAMPAIGN_EDIT', '5FDB6F28-10E5-55FD-8B8F-B47655368BC8', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('E72552D6-1358-507A-AC98-1450867C0ADB', N'删除专题活动', 'MALL_CAMPAIGN_DELETE', '5FDB6F28-10E5-55FD-8B8F-B47655368BC8', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('FB38A9D3-9AD8-5E44-8404-217D043298C5', N'新增优惠券', 'MALL_COUPON_ADD', 'E964B0FE-48A6-51C9-8E79-236966FDB010', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('FEB8685F-3C94-56FE-A634-FE18BC1044E5', N'编辑优惠券', 'MALL_COUPON_EDIT', 'E964B0FE-48A6-51C9-8E79-236966FDB010', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('4CF697E9-0FE0-5513-8119-4E52EF97DF73', N'删除优惠券', 'MALL_COUPON_DELETE', 'E964B0FE-48A6-51C9-8E79-236966FDB010', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('1C10D83D-07A2-5EDE-BC2D-9CEBF9D992DB', N'新增 Banner', 'MALL_BANNER_ADD', 'E3286CAC-A365-56A9-BF2D-4EE6BFCB00CE', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('A25DFC28-ABD1-5757-A0B1-8C807077F518', N'编辑 Banner', 'MALL_BANNER_EDIT', 'E3286CAC-A365-56A9-BF2D-4EE6BFCB00CE', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('2667581F-164D-548A-83D9-FCB3D6789E49', N'删除 Banner', 'MALL_BANNER_DELETE', 'E3286CAC-A365-56A9-BF2D-4EE6BFCB00CE', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('457F38B9-D16C-5DD0-9362-22EB6CC44931', N'处理订单', 'MALL_ORDER_EDIT', 'A6920F3A-9FE8-571C-ADE8-ED364DEBE71F', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('D44ABC6F-C1A3-57AF-89EB-B6AD9510C5D6', N'处理发货', 'MALL_SHIPMENT_EDIT', '5C511A49-AA96-585B-9810-DF7C436B83FC', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('C5247973-9116-59D0-BBE2-76E7F260430B', N'处理退货单', 'MALL_RETURN_ORDER_EDIT', '4CF764F7-D12F-52B5-A3AB-0CBC65EC8A5A', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('B14BC089-262D-5904-B121-BB03E4A0D956', N'新增商城门店', 'MALL_STORE_ADD', '3393426C-7901-5991-8286-EF93E2BC6DF9', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('340454D4-AA2F-56C2-9536-141B2BBF1799', N'编辑商城门店', 'MALL_STORE_EDIT', '3393426C-7901-5991-8286-EF93E2BC6DF9', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('AAB0EA48-FC8E-56F0-B4BA-F931F6E0917D', N'删除商城门店', 'MALL_STORE_DELETE', '3393426C-7901-5991-8286-EF93E2BC6DF9', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('2BC52DF2-0673-5CEA-A3D7-86968BCC1166', N'新增运费规则', 'MALL_FREIGHT_ADD', 'C1F11E0D-C9B2-52A4-95C8-A486F831FD26', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('D511958A-AB75-505E-8B98-6299546A7150', N'编辑运费规则', 'MALL_FREIGHT_EDIT', 'C1F11E0D-C9B2-52A4-95C8-A486F831FD26', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('D2FE7375-50B1-57CB-8C51-12EDE47D5080', N'删除运费规则', 'MALL_FREIGHT_DELETE', 'C1F11E0D-C9B2-52A4-95C8-A486F831FD26', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('4D27638F-9D50-5B17-9179-ED899345AC0C', N'系统设置', 'SYSTEM', '3C70E31D-CAB2-5B58-B380-59B40B421CDC', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('56D58D3E-5AB0-5D8D-8245-22943F28454C', N'角色设置', 'SYSTEM_ROLE', '4D27638F-9D50-5B17-9179-ED899345AC0C', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('9FE6F1E7-4CE2-5281-9AB9-7CA19F1FEA07', N'权限设置', 'SYSTEM_PERMISSION', '4D27638F-9D50-5B17-9179-ED899345AC0C', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('DC460291-6A3E-5352-A04F-9D0976DC06AD', N'用户管理', 'SYSTEM_USER', '4D27638F-9D50-5B17-9179-ED899345AC0C', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('85407D20-263F-5168-8DA1-349C109D0466', N'数据字典', 'SYSTEM_DATA_DICTIONARY', '4D27638F-9D50-5B17-9179-ED899345AC0C', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('495D1AB9-C4F4-5617-AF97-B2CD504AB990', N'地址区域维护', 'SYSTEM_REGION', '4D27638F-9D50-5B17-9179-ED899345AC0C', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('FD5C9A57-742C-516D-8E62-6553ECF04CBF', N'操作日志', 'SYSTEM_OPERATION_LOG', '4D27638F-9D50-5B17-9179-ED899345AC0C', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('31F5A949-67FA-55D2-A5FB-59D837B1FFBB', N'错误日志', 'SYSTEM_ERROR_LOG', '4D27638F-9D50-5B17-9179-ED899345AC0C', N'Menu', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('2D3B8DCF-A97C-5CB2-82D6-964A1AFA2062', N'新增用户', 'SYSTEM_USER_ADD', 'DC460291-6A3E-5352-A04F-9D0976DC06AD', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('07F19A18-3561-5DA4-B98F-57316E5A1A63', N'编辑用户', 'SYSTEM_USER_EDIT', 'DC460291-6A3E-5352-A04F-9D0976DC06AD', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('D4F19247-ECD7-5349-9C4F-598BABAFF7D3', N'新增角色', 'SYSTEM_ROLE_ADD', '56D58D3E-5AB0-5D8D-8245-22943F28454C', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('A0C9A3D7-4C86-5FB1-8181-6DE1D81F1607', N'编辑角色', 'SYSTEM_ROLE_EDIT', '56D58D3E-5AB0-5D8D-8245-22943F28454C', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('FCB1B652-9AEB-565C-A62E-A5575BED0E9D', N'删除角色', 'SYSTEM_ROLE_DELETE', '56D58D3E-5AB0-5D8D-8245-22943F28454C', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('894E448B-CBC2-52EA-909A-ABECD0583AFD', N'编辑权限', 'SYSTEM_PERMISSION_EDIT', '9FE6F1E7-4CE2-5281-9AB9-7CA19F1FEA07', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('1C189A74-9EAE-5D79-84A0-F8C85AE0B946', N'新增字典', 'SYSTEM_DATA_DICTIONARY_ADD', '85407D20-263F-5168-8DA1-349C109D0466', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('669012C9-3F5D-5D84-BB0A-0F356C5C2759', N'编辑字典', 'SYSTEM_DATA_DICTIONARY_EDIT', '85407D20-263F-5168-8DA1-349C109D0466', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0),
('60CBA592-723E-56D2-9673-DA450FB822D8', N'删除字典', 'SYSTEM_DATA_DICTIONARY_DELETE', '85407D20-263F-5168-8DA1-349C109D0466', N'Button', GETDATE(), N'seed', GETDATE(), N'seed', 0);

-- SystemRolePermissions (120 rows) admin 全量 / user 仅菜单级
INSERT INTO [dbo].[SystemRolePermissions] ([Id], [RoleId], [PermissionId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted]) VALUES
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '3C70E31D-CAB2-5B58-B380-59B40B421CDC', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'D3D4D9F8-C36B-58C1-BC49-A7278A2A6C7C', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '3EF2F682-24F6-596F-935D-9E5F0C9D9705', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'E1D5A232-0D1A-53D3-A62B-CD63A7357A83', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'D72D8995-98B3-5451-9543-FB864A335589', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'F456E35C-33CF-57CC-81C7-7F64AC416081', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'C5041A6A-65A5-5699-B744-0B6921C011D8', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'A8623F7A-A38A-52F8-A14B-B4F9BC7C86E5', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '08DCE4B9-8F1C-56E2-955E-A715C2BCA5AB', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '5B2C3350-CEED-5AB7-B870-D2BEB15DB7D2', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'FAE40574-8112-58D8-B23B-82D0D220AEAA', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '0DB7C19E-D26D-5F75-8240-C15E52B57929', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '3DD66318-153A-5985-BFBC-560C5950A7AB', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'C08A6930-E16A-54AE-8672-CE867559D624', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '71A18294-EBC8-55AA-A49A-752EFE8EC220', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '775B5324-252F-5262-88BC-00C93D27EEA8', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'E5962908-7B07-5DC9-A983-30A706BDAD32', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '7FE63AA3-3D2D-5EBB-B668-6DAB925D2506', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'DB439BA0-6985-56DC-9523-21BD5061767D', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'F6E7F82C-0A0F-52EB-93D7-A432183566BA', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '5EFA101B-2C7B-5682-82E4-279C5EFEEAF5', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'B935D9CC-D9AE-5605-9F90-7562992792BC', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '8DDF8455-8410-5AFC-896E-1FEE8DB8FFD4', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'A73C26FC-02C7-59C3-9598-5ADDDD006236', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '183B65E4-74E1-57B7-AF30-9C59D261905B', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '20E7DFC8-EEEF-5E3C-98A7-8A7316C77A08', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '9DBC0016-B674-55A9-A150-AA1E34A53319', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '8A433F7E-8FDC-5B18-AA99-15E555A1C129', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '140CEDD0-5C87-5F76-92F9-48FE5B8EFA87', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '5FDB6F28-10E5-55FD-8B8F-B47655368BC8', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'E964B0FE-48A6-51C9-8E79-236966FDB010', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'E3286CAC-A365-56A9-BF2D-4EE6BFCB00CE', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'A6920F3A-9FE8-571C-ADE8-ED364DEBE71F', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '5C511A49-AA96-585B-9810-DF7C436B83FC', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '4CF764F7-D12F-52B5-A3AB-0CBC65EC8A5A', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '3393426C-7901-5991-8286-EF93E2BC6DF9', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'C1F11E0D-C9B2-52A4-95C8-A486F831FD26', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'A76BEDC8-6A08-557A-90DC-2D628BE83377', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '127D7E3E-F53E-5ADD-B869-561D4C168F29', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'E7BE1B6D-E675-5013-8203-417C2DEE49D6', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'A526DB8D-64D8-5AE6-B92C-DD94694F6C48', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'B9BFB39C-E33D-55C6-8447-D4EEF4026D0E', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '506F05F4-E874-5989-AE9B-EA7FDB322BD0', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'F578ED5F-8DF2-5C79-A7A9-BA00DB09B052', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '3DC70916-5B6E-5D7A-9AAE-B06B257F3ADE', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '8CA7B98F-0308-5BCD-8CF1-3981B6C54256', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '633FED4D-D577-567B-98E2-D56DFB1F2BFE', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'BB302D03-B0CE-56D3-97B2-A949EACB0705', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'F0CD7FBC-7B8F-579C-BB91-D086F36788E1', GETDATE(), N'seed', GETDATE(), N'seed', 0);
INSERT INTO [dbo].[SystemRolePermissions] ([Id], [RoleId], [PermissionId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted]) VALUES
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '6C747389-B4E3-5169-A2D5-3A0807008266', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '83CE5522-6C1B-5479-8DA6-AA98593B7599', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '15B779FF-63AD-5A93-860C-6D5B68B78A6E', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'BAECB234-2285-5D01-A73E-579FBCB299EC', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '3BE8D281-033D-5729-9F83-048BBB149436', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'E72552D6-1358-507A-AC98-1450867C0ADB', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'FB38A9D3-9AD8-5E44-8404-217D043298C5', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'FEB8685F-3C94-56FE-A634-FE18BC1044E5', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '4CF697E9-0FE0-5513-8119-4E52EF97DF73', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '1C10D83D-07A2-5EDE-BC2D-9CEBF9D992DB', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'A25DFC28-ABD1-5757-A0B1-8C807077F518', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '2667581F-164D-548A-83D9-FCB3D6789E49', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '457F38B9-D16C-5DD0-9362-22EB6CC44931', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'D44ABC6F-C1A3-57AF-89EB-B6AD9510C5D6', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'C5247973-9116-59D0-BBE2-76E7F260430B', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'B14BC089-262D-5904-B121-BB03E4A0D956', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '340454D4-AA2F-56C2-9536-141B2BBF1799', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'AAB0EA48-FC8E-56F0-B4BA-F931F6E0917D', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '2BC52DF2-0673-5CEA-A3D7-86968BCC1166', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'D511958A-AB75-505E-8B98-6299546A7150', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'D2FE7375-50B1-57CB-8C51-12EDE47D5080', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '4D27638F-9D50-5B17-9179-ED899345AC0C', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '56D58D3E-5AB0-5D8D-8245-22943F28454C', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '9FE6F1E7-4CE2-5281-9AB9-7CA19F1FEA07', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'DC460291-6A3E-5352-A04F-9D0976DC06AD', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '85407D20-263F-5168-8DA1-349C109D0466', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '495D1AB9-C4F4-5617-AF97-B2CD504AB990', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'FD5C9A57-742C-516D-8E62-6553ECF04CBF', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '31F5A949-67FA-55D2-A5FB-59D837B1FFBB', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '2D3B8DCF-A97C-5CB2-82D6-964A1AFA2062', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '07F19A18-3561-5DA4-B98F-57316E5A1A63', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'D4F19247-ECD7-5349-9C4F-598BABAFF7D3', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'A0C9A3D7-4C86-5FB1-8181-6DE1D81F1607', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', 'FCB1B652-9AEB-565C-A62E-A5575BED0E9D', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '894E448B-CBC2-52EA-909A-ABECD0583AFD', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '1C189A74-9EAE-5D79-84A0-F8C85AE0B946', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '669012C9-3F5D-5D84-BB0A-0F356C5C2759', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), 'A6291778-0A2F-4235-BC36-5675D897ABAF', '60CBA592-723E-56D2-9673-DA450FB822D8', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '3C70E31D-CAB2-5B58-B380-59B40B421CDC', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', 'D3D4D9F8-C36B-58C1-BC49-A7278A2A6C7C', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '3EF2F682-24F6-596F-935D-9E5F0C9D9705', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', 'E1D5A232-0D1A-53D3-A62B-CD63A7357A83', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', 'D72D8995-98B3-5451-9543-FB864A335589', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', 'F456E35C-33CF-57CC-81C7-7F64AC416081', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', 'C5041A6A-65A5-5699-B744-0B6921C011D8', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', 'A8623F7A-A38A-52F8-A14B-B4F9BC7C86E5', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '08DCE4B9-8F1C-56E2-955E-A715C2BCA5AB', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '8C772F15-36AE-57BB-8CDE-EA6AA429A2CB', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', 'A73C26FC-02C7-59C3-9598-5ADDDD006236', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '183B65E4-74E1-57B7-AF30-9C59D261905B', GETDATE(), N'seed', GETDATE(), N'seed', 0);
INSERT INTO [dbo].[SystemRolePermissions] ([Id], [RoleId], [PermissionId], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted]) VALUES
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '20E7DFC8-EEEF-5E3C-98A7-8A7316C77A08', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '9DBC0016-B674-55A9-A150-AA1E34A53319', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '8A433F7E-8FDC-5B18-AA99-15E555A1C129', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '140CEDD0-5C87-5F76-92F9-48FE5B8EFA87', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '5FDB6F28-10E5-55FD-8B8F-B47655368BC8', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', 'E964B0FE-48A6-51C9-8E79-236966FDB010', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', 'E3286CAC-A365-56A9-BF2D-4EE6BFCB00CE', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', 'A6920F3A-9FE8-571C-ADE8-ED364DEBE71F', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '5C511A49-AA96-585B-9810-DF7C436B83FC', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '4CF764F7-D12F-52B5-A3AB-0CBC65EC8A5A', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '3393426C-7901-5991-8286-EF93E2BC6DF9', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', 'C1F11E0D-C9B2-52A4-95C8-A486F831FD26', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '4D27638F-9D50-5B17-9179-ED899345AC0C', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '56D58D3E-5AB0-5D8D-8245-22943F28454C', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '9FE6F1E7-4CE2-5281-9AB9-7CA19F1FEA07', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', 'DC460291-6A3E-5352-A04F-9D0976DC06AD', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '85407D20-263F-5168-8DA1-349C109D0466', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '495D1AB9-C4F4-5617-AF97-B2CD504AB990', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', 'FD5C9A57-742C-516D-8E62-6553ECF04CBF', GETDATE(), N'seed', GETDATE(), N'seed', 0),
(NEWID(), '46C37FC5-1BCF-4747-A71C-2F9041243CD3', '31F5A949-67FA-55D2-A5FB-59D837B1FFBB', GETDATE(), N'seed', GETDATE(), N'seed', 0);

-- SystemDataDictionaries (27 rows)
INSERT INTO [dbo].[SystemDataDictionaries] ([Id], [ParentId], [Code], [Name], [Value], [Description], [SortOrder], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted]) VALUES
('A82B3B44-8DF6-456B-AB90-0ED5E5970438', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '黄连', '黄连', '黄连', '测试品类', 12, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('2DAE6BAE-942C-45AB-A86E-1CAFC017043D', NULL, 'CRM_HERB_PRODUCT', '中药材品类', '中药材品类', 'CRM 品类统一维护根节点', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('BB93476A-D1D0-46A3-869A-26B56AE08562', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '五味子', '五味子', '五味子', '测试品类', 23, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('5CC78642-1F49-4B89-8ADA-33EA2E9F8776', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '黄芩', '黄芩', '黄芩', '测试品类', 19, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('14D2DAE9-827B-4759-8311-37B1931920AF', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '陈皮', '陈皮', '陈皮', '测试品类', 22, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('FE5E6040-756A-44D0-9386-49737E2712A1', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '麦冬', '麦冬', '麦冬', '测试品类', 20, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('D9641CB8-EB62-47EA-83C0-499FE984FA7C', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '柴胡', '柴胡', '柴胡', '测试品类', 18, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('60BF4A2A-A7BD-4E8A-931C-50B7CE9E470B', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '丹参', '丹参', '丹参', '测试品类', 7, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('0198FAD6-5771-47EF-8961-5B069C360860', NULL, 'GOV_HERB_BASE', '政府网站', 'GOV_HERB_BASE', '政府公开网站来源', 1, 1, '2026-10-31 16:30:41.7062648', '46581FD2-2A4F-4CAC-A6FB-E01A09BA5AC9', '2026-10-31 16:30:41.7062648', '46581FD2-2A4F-4CAC-A6FB-E01A09BA5AC9', 0),
('4FEF206E-05A4-4429-A4AA-5BC89CF3F4AC', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '党参', '党参', '党参', '测试品类', 3, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('4AD006BD-ECCF-4A9A-AF17-67127D9CD490', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '甘草', '甘草', '甘草', '测试品类', 4, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('7440B83F-504B-41BA-AD24-75534DFBC659', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '砂仁', '砂仁', '砂仁', '测试品类', 21, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('3613D0F3-F179-4CFD-9667-7C205E2AD340', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '黄芪', '黄芪', '黄芪', '测试品类', 1, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('6E85F3DA-5A24-4AF5-AEAC-93A2172A1AD5', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '桂枝', '桂枝', '桂枝', '测试品类', 25, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('C86D0D27-4375-4422-A61C-BB3138BE9E17', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '红枣', '红枣', '红枣', '测试品类', 17, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('FE1FD034-AD52-43FC-9A82-BC909BF08CE3', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '当归', '当归', '当归', '测试品类', 2, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('AF969D23-E96F-4F5C-BD66-BCFC0FAECB37', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '白术', '白术', '白术', '测试品类', 11, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('BE665826-888D-4B74-8B1F-CDB378D82ABC', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '金银花', '金银花', '金银花', '测试品类', 6, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('EE25A4D2-AF39-42F5-A7A9-CDDA99814ECF', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '白芍', '白芍', '白芍', '测试品类', 8, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('A9E21988-7D16-4FB4-9E53-E78BB1D9BD2C', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '枸杞', '枸杞', '枸杞', '测试品类', 5, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('A9CF6DEA-68EB-424D-AF5E-EC660AD2C06A', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '莲子', '莲子', '莲子', '测试品类', 16, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('B0DD1905-0BB6-4C12-830D-EF292912629B', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '天麻', '天麻', '天麻', '测试品类', 14, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('D6771100-FA32-42F9-9C97-F1C1B81960F2', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '山药', '山药', '山药', '测试品类', 15, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('5CDFB21E-1F55-452C-A206-F4079D506D23', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '川芎', '川芎', '川芎', '测试品类', 9, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('D1B9AC1C-9154-446B-88E1-F68615635512', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '三七', '三七', '三七', '测试品类', 13, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('61625B79-C6C6-42DA-8798-FABF18B14D5A', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '杜仲', '杜仲', '杜仲', '测试品类', 24, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('9E4E274C-88A6-4B9C-BEB0-FD57E19AAA7F', '2DAE6BAE-942C-45AB-A86E-1CAFC017043D', '茯苓', '茯苓', '茯苓', '测试品类', 10, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0);

-- SystemChinaRegions (124 rows)
INSERT INTO [dbo].[SystemChinaRegions] ([Id], [Code], [Name], [FullName], [Level], [ParentCode], [ProvinceCode], [CityCode], [SortOrder], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted]) VALUES
('FBDF4E80-C0F0-41D2-BED6-022BBCE59CDC', '621100', '定西市', '甘肃省定西市', 2, '620000', '620000', '621100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('239E4AB0-B674-4E96-A854-02CF5397E8CD', '650100', '乌鲁木齐市', '新疆维吾尔自治区乌鲁木齐市', 2, '650000', '650000', '650100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('64E04BF4-030B-4D32-82D5-02EF7E332A32', '411302', '宛城区', '河南省南阳市宛城区', 3, '411300', '410000', '411300', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('86B6CE4B-2D6F-4824-92E7-0514DA9E67D4', '310000', '上海市', '上海市', 1, NULL, '310000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('EB38CF05-E17A-48B0-B240-06D468B911D1', '330000', '浙江省', '浙江省', 1, NULL, '330000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('053DB046-445B-45F6-97C0-0AF835AE051D', '410102', '中原区', '河南省郑州市中原区', 3, '410100', '410000', '410100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('1B873D1E-FB2A-46B4-9A35-1180B667D786', '620000', '甘肃省', '甘肃省', 1, NULL, '620000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('6E87CB8C-A594-41C4-B1F4-13AD204F21F5', '210100', '沈阳市', '辽宁省沈阳市', 2, '210000', '210000', '210100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('D8F36FE9-7914-4C16-9138-1448388C62EB', '310101', '黄浦区', '上海市上海市黄浦区', 3, '310100', '310000', '310100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('78A8E3E5-BBD4-4B68-936F-1CCA42080BA0', '450102', '兴宁区', '广西壮族自治区南宁市兴宁区', 3, '450100', '450000', '450100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('E14B8A9E-5DE7-44D7-9F9E-21BAB5CC8423', '130000', '河北省', '河北省', 1, NULL, '130000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('F70A1C64-03F5-4357-8AB6-225F6433542A', '610100', '西安市', '陕西省西安市', 2, '610000', '610000', '610100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('CA9CD5A0-DC75-4FC1-84FA-242DDEE7FDDE', '360000', '江西省', '江西省', 1, NULL, '360000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('B07D1F06-9929-4509-9042-2453E7F1E5F3', '650000', '新疆维吾尔自治区', '新疆维吾尔自治区', 1, NULL, '650000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('7AC2FD3F-BF49-4C95-B7F6-28011D9B8408', '510100', '成都市', '四川省成都市', 2, '510000', '510000', '510100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('03561054-8121-43AD-9ABC-2D26C707A033', '510700', '绵阳市', '四川省绵阳市', 2, '510000', '510000', '510700', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('8B25B5E5-F27E-4EDB-BE69-2E33A52CA93F', '530800', '普洱市', '云南省普洱市', 2, '530000', '530000', '530800', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('16DE83A9-F82B-4AC4-9801-2F9E3CD99E74', '350000', '福建省', '福建省', 1, NULL, '350000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('222A01FE-72A3-4F24-8BFC-312C5F62FB79', '630102', '城东区', '青海省西宁市城东区', 3, '630100', '630000', '630100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('9EAA7E30-5C17-465B-B884-3413E62EA588', '210000', '辽宁省', '辽宁省', 1, NULL, '210000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('2D91A3AA-7E9C-4119-8128-3AD3D1D88A33', '130600', '保定市', '河北省保定市', 2, '130000', '130000', '130600', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('055F72AE-1173-442A-9101-3AF7D643F962', '640000', '宁夏回族自治区', '宁夏回族自治区', 1, NULL, '640000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('01B9C87E-7848-4B36-8D99-3B68BFBAEC06', '230000', '黑龙江省', '黑龙江省', 1, NULL, '230000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('07FC1FE2-1C7A-4AD4-ABE6-3C08C8182F87', '430102', '芙蓉区', '湖南省长沙市芙蓉区', 3, '430100', '430000', '430100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('B00502E6-26B9-4C52-A04A-4019D0165CF9', '410000', '河南省', '河南省', 1, NULL, '410000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('83192499-CF74-40A2-88F3-404CBC3D1B1D', '420100', '武汉市', '湖北省武汉市', 2, '420000', '420000', '420100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('F7A645AB-4003-4727-A623-441E690E0EDB', '320000', '江苏省', '江苏省', 1, NULL, '320000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('F59BC6E1-391A-462C-9ECE-4434906CFCE1', '320100', '南京市', '江苏省南京市', 2, '320000', '320000', '320100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('A234866A-03E0-476B-9B85-4545C54817E9', '330102', '上城区', '浙江省杭州市上城区', 3, '330100', '330000', '330100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('5259BA2F-9F6D-4306-BF9C-4631C938CAAE', '340102', '瑶海区', '安徽省合肥市瑶海区', 3, '340100', '340000', '340100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('A41A1AAE-3835-4B89-ABA9-4759931784D1', '410302', '老城区', '河南省洛阳市老城区', 3, '410300', '410000', '410300', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('F79C48D0-8870-443B-89C8-4A68E46FBC77', '540000', '西藏自治区', '西藏自治区', 1, NULL, '540000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('1F5C7361-FF93-4C2B-8C2D-4B98D559E5C7', '110108', '海淀区', '北京市北京市海淀区', 3, '110100', '110000', '110100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('A15A09AE-7E89-4F01-AAFC-4EE75EB995C9', '350102', '鼓楼区', '福建省福州市鼓楼区', 3, '350100', '350000', '350100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('9A8CE4BE-5790-4678-BE6A-4F2DC0F494C6', '320500', '苏州市', '江苏省苏州市', 2, '320000', '320000', '320500', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('E8D9486C-61B8-47E5-9087-50EB4B671255', '530100', '昆明市', '云南省昆明市', 2, '530000', '530000', '530100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('9952F112-4C70-4FA7-BB80-51F2A1371608', '220100', '长春市', '吉林省长春市', 2, '220000', '220000', '220100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('87E110BF-0352-438B-9C67-563671E0813D', '320505', '虎丘区', '江苏省苏州市虎丘区', 3, '320500', '320000', '320500', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('8303E405-5989-4FAB-85CC-56ECC92BBA6A', '140000', '山西省', '山西省', 1, NULL, '140000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('A96A711C-7A16-4AA1-80B7-60F200D7696C', '230102', '道里区', '黑龙江省哈尔滨市道里区', 3, '230100', '230000', '230100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('9811D56D-A1E0-4244-97D3-62F286201704', '370202', '市南区', '山东省青岛市市南区', 3, '370200', '370000', '370200', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('DCDF41DD-4F6E-4D0E-9682-65C67146A7EB', '640104', '兴庆区', '宁夏回族自治区银川市兴庆区', 3, '640100', '640000', '640100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('9F9E8B45-3C7A-4997-A627-665F74CA785C', '410300', '洛阳市', '河南省洛阳市', 2, '410000', '410000', '410300', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('D7F84458-5E58-4139-954A-669C9CE4C58B', '520000', '贵州省', '贵州省', 1, NULL, '520000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('A286E93D-B185-480A-9925-68FC1BF99A32', '140105', '小店区', '山西省太原市小店区', 3, '140100', '140000', '140100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('883FE606-5B9B-4845-8E2F-69DB38770985', '540102', '城关区', '西藏自治区拉萨市城关区', 3, '540100', '540000', '540100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('615C53DA-54BD-4A89-A186-6AFAD00987B6', '330100', '杭州市', '浙江省杭州市', 2, '330000', '330000', '330100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('E879A007-7556-4D5E-B8A5-6D71C345979B', '360700', '赣州市', '江西省赣州市', 2, '360000', '360000', '360700', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('085400A7-5DE2-4E34-BCB8-785791D673E3', '350100', '福州市', '福建省福州市', 2, '350000', '350000', '350100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('116DD461-BC8B-4570-BB9B-799CEA7712AB', '310115', '浦东新区', '上海市上海市浦东新区', 3, '310100', '310000', '310100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0);
INSERT INTO [dbo].[SystemChinaRegions] ([Id], [Code], [Name], [FullName], [Level], [ParentCode], [ProvinceCode], [CityCode], [SortOrder], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted]) VALUES
('142D58EA-80F2-4F78-93A3-7C4BEE1CEEFE', '650102', '天山区', '新疆维吾尔自治区乌鲁木齐市天山区', 3, '650100', '650000', '650100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('05A11FD0-1BAA-47F2-88C6-8067782A1203', '130402', '邯山区', '河北省邯郸市邯山区', 3, '130400', '130000', '130400', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('66F419D9-9E74-4E5A-B966-81ED234E5DAD', '510703', '涪城区', '四川省绵阳市涪城区', 3, '510700', '510000', '510700', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('1B90D99C-A400-4D7F-AA7F-861E01F2A5DF', '130602', '竞秀区', '河北省保定市竞秀区', 3, '130600', '130000', '130600', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('15605A9F-C900-4D3E-9B37-8713A1917DC2', '621102', '安定区', '甘肃省定西市安定区', 3, '621100', '620000', '621100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('5F41EEC5-204A-491E-8C87-886B82DFD06E', '320102', '玄武区', '江苏省南京市玄武区', 3, '320100', '320000', '320100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('5194E4E5-B063-4780-9A13-89CCC2C53228', '130100', '石家庄市', '河北省石家庄市', 2, '130000', '130000', '130100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('61F5E8D4-EE0B-4BC2-ADB5-8AD5AF1412C3', '110102', '西城区', '北京市北京市西城区', 3, '110100', '110000', '110100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('B05F458E-6A41-4147-8CFE-8BF40E3CFAD3', '340802', '迎江区', '安徽省安庆市迎江区', 3, '340800', '340000', '340800', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('C62CBB3E-CBD0-4266-B11C-8DB66104158F', '370000', '山东省', '山东省', 1, NULL, '370000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('EABE0A05-CF95-4700-A860-9192151D6F7B', '640100', '银川市', '宁夏回族自治区银川市', 2, '640000', '640000', '640100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('C3C69A84-E808-4775-8A6D-91E6C83E8B99', '330302', '鹿城区', '浙江省温州市鹿城区', 3, '330300', '330000', '330300', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('F262AD6D-D774-4194-8790-94BFA35F6662', '410100', '郑州市', '河南省郑州市', 2, '410000', '410000', '410100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('5D6BB663-9A9E-441B-8E7C-9544E92A4089', '130400', '邯郸市', '河北省邯郸市', 2, '130000', '130000', '130400', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('4786D000-A93C-4166-8051-98C83D3A45EC', '370102', '历下区', '山东省济南市历下区', 3, '370100', '370000', '370100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('4F1CA9EC-C44E-4C58-A84A-9917CCAD046E', '520102', '南明区', '贵州省贵阳市南明区', 3, '520100', '520000', '520100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('6CB39901-8AF7-4AAE-A71E-9967401FCB98', '440000', '广东省', '广东省', 1, NULL, '440000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('83A0286F-77A3-4944-B9D2-99D6716B5660', '420000', '湖北省', '湖北省', 1, NULL, '420000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('C402A6A9-3137-4C45-BDCD-9B181C801331', '330203', '海曙区', '浙江省宁波市海曙区', 3, '330200', '330000', '330200', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('21619313-EB63-43A8-8B1B-9B4140FCED2B', '440304', '福田区', '广东省深圳市福田区', 3, '440300', '440000', '440300', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('3156028D-1AAE-40C0-AF5A-9C519BE77BFB', '460105', '秀英区', '海南省海口市秀英区', 3, '460100', '460000', '460100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('978C1197-BA84-49A6-B95F-9CD6C4587211', '430000', '湖南省', '湖南省', 1, NULL, '430000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('5800A0A4-7735-41FC-A127-9EDD86E9184E', '530802', '思茅区', '云南省普洱市思茅区', 3, '530800', '530000', '530800', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('DD828007-8E92-4875-A683-9F755B61EB9D', '411300', '南阳市', '河南省南阳市', 2, '410000', '410000', '411300', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('B56143BD-069E-410B-B556-9F97F04B95BA', '440300', '深圳市', '广东省深圳市', 2, '440000', '440000', '440300', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('1CB09423-EEF1-4479-8C4E-A19CE01F7EEA', '620100', '兰州市', '甘肃省兰州市', 2, '620000', '620000', '620100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('D1481C60-3D2C-465D-B527-A32FF7E6B678', '420102', '江岸区', '湖北省武汉市江岸区', 3, '420100', '420000', '420100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('BB9694C6-915F-4180-85AA-A7EC65C38288', '500101', '万州区', '重庆市重庆市万州区', 3, '500100', '500000', '500100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('A027BD78-74E7-4C52-969A-A8910446FBF5', '440100', '广州市', '广东省广州市', 2, '440000', '440000', '440100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('4F7F103B-791C-463B-AB69-A9E7F1505D53', '340600', '亳州市', '安徽省亳州市', 2, '340000', '340000', '340600', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('91973764-9283-4E2D-B7EB-AB57ACF0A683', '130102', '长安区', '河北省石家庄市长安区', 3, '130100', '130000', '130100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('C8413DD9-8F67-40B0-A499-B0D2FA7E2BA8', '310104', '徐汇区', '上海市上海市徐汇区', 3, '310100', '310000', '310100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('1034735A-97A6-464F-9954-B0E16CB6CAB9', '530102', '五华区', '云南省昆明市五华区', 3, '530100', '530000', '530100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('0A1408E5-6FEA-4E58-870D-B1D95FEF3DBD', '520100', '贵阳市', '贵州省贵阳市', 2, '520000', '520000', '520100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('4854F4B1-AF4F-4A18-BB43-B4F6E5F83332', '510104', '锦江区', '四川省成都市锦江区', 3, '510100', '510000', '510100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('F951B798-87A9-4947-8447-B7DBFA976040', '340800', '安庆市', '安徽省安庆市', 2, '340000', '340000', '340800', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('F21912B3-BE25-4007-AD43-BE531596839F', '430100', '长沙市', '湖南省长沙市', 2, '430000', '430000', '430100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('E869E40E-E280-4623-8FE3-BEC1FC014381', '310100', '上海市', '上海市上海市', 2, '310000', '310000', '310100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('F2B931B9-70D5-4575-9AE9-C0E9DCCBE410', '341602', '谯城区', '安徽省亳州市谯城区', 3, '340600', '340000', '340600', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('99B5E24B-A045-4E91-8443-C2D019A7E721', '630000', '青海省', '青海省', 1, NULL, '630000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('49C521F5-2F7D-46AE-88F6-C34EDD19DCCD', '370100', '济南市', '山东省济南市', 2, '370000', '370000', '370100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('96C5AD20-4E78-4FF5-B7CC-C3D8B12E035C', '440103', '荔湾区', '广东省广州市荔湾区', 3, '440100', '440000', '440100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('704A905F-F864-47D6-A15C-C41C01F488FE', '210102', '和平区', '辽宁省沈阳市和平区', 3, '210100', '210000', '210100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('17E24D82-9984-40EB-926F-C8681071F5D2', '230100', '哈尔滨市', '黑龙江省哈尔滨市', 2, '230000', '230000', '230100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('AE352521-3643-430D-8BB1-C964F433C111', '610000', '陕西省', '陕西省', 1, NULL, '610000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('4F75F21E-F222-412B-B908-CDD75504DC70', '540100', '拉萨市', '西藏自治区拉萨市', 2, '540000', '540000', '540100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('E334DAD4-DAEF-4F00-AE79-CFAF48FA0122', '460000', '海南省', '海南省', 1, NULL, '460000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('FFC52F61-2980-4191-B92C-CFD04C40C9B0', '630100', '西宁市', '青海省西宁市', 2, '630000', '630000', '630100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('7164D0AD-7C31-4F8C-8CB2-D0AD047C3442', '110101', '东城区', '北京市北京市东城区', 3, '110100', '110000', '110100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('CF9148AE-E9AF-465D-A9CB-D564BED47014', '340100', '合肥市', '安徽省合肥市', 2, '340000', '340000', '340100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0);
INSERT INTO [dbo].[SystemChinaRegions] ([Id], [Code], [Name], [FullName], [Level], [ParentCode], [ProvinceCode], [CityCode], [SortOrder], [IsActive], [CreatedAt], [CreatedBy], [UpdatedAt], [UpdatedBy], [IsDeleted]) VALUES
('308B3332-E923-40DC-AA11-D693EA544DD7', '530000', '云南省', '云南省', 1, NULL, '530000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('8C097DC3-AAC0-47EB-BC9E-DC27DA234F05', '610102', '新城区', '陕西省西安市新城区', 3, '610100', '610000', '610100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('5B8EBB4E-F23C-457D-8CD0-DDE2F06DD975', '110105', '朝阳区', '北京市北京市朝阳区', 3, '110100', '110000', '110100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('DB6C8EF2-D062-4FF6-974F-E056E832AC2D', '110000', '北京市', '北京市', 1, NULL, '110000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('900EE9A3-DCBB-4689-B3C3-E4BF35BEE1CF', '500000', '重庆市', '重庆市', 1, NULL, '500000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('A9FA6D34-E760-4120-8029-E58C09907FC2', '330300', '温州市', '浙江省温州市', 2, '330000', '330000', '330300', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('E447FAB4-BAF7-4BC8-8F5F-E64A0CEAAF86', '360100', '南昌市', '江西省南昌市', 2, '360000', '360000', '360100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('FFD39062-E0B8-49FE-A013-E6F501386787', '110100', '北京市', '北京市北京市', 2, '110000', '110000', '110100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('854C25ED-A386-46C9-9AB1-E7C0B3E4D443', '330200', '宁波市', '浙江省宁波市', 2, '330000', '330000', '330200', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('23DA6B79-4674-4EF0-B87B-E855F6ED1378', '350200', '厦门市', '福建省厦门市', 2, '350000', '350000', '350200', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('74520955-1F68-48E0-929F-E8A7AFCF243C', '370200', '青岛市', '山东省青岛市', 2, '370000', '370000', '370200', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('489E2020-B9EE-4916-B4FD-E941A370C477', '510000', '四川省', '四川省', 1, NULL, '510000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('821FF5B6-8FF2-4E55-904D-E980AC179A1F', '140100', '太原市', '山西省太原市', 2, '140000', '140000', '140100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('A26CECC5-63BB-481A-86F0-EFA704A1EFC4', '460100', '海口市', '海南省海口市', 2, '460000', '460000', '460100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('12CDF89C-BF5C-49CA-A442-F200E86B5E34', '360102', '东湖区', '江西省南昌市东湖区', 3, '360100', '360000', '360100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('804B4DCD-AFBE-433A-A872-F2D4D408568F', '450100', '南宁市', '广西壮族自治区南宁市', 2, '450000', '450000', '450100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('6FE024F5-4009-49AA-B4A4-F657C93FF6B2', '620102', '城关区', '甘肃省兰州市城关区', 3, '620100', '620000', '620100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('A3071581-8F5C-4905-8365-F72F456D1C71', '220102', '南关区', '吉林省长春市南关区', 3, '220100', '220000', '220100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('87007E72-7A4E-4503-8A1E-FB665F870378', '500100', '重庆市', '重庆市重庆市', 2, '500000', '500000', '500100', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('822B3118-59D7-462B-9058-FCB27BD048A5', '360702', '章贡区', '江西省赣州市章贡区', 3, '360700', '360000', '360700', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('043E1506-AEE9-4DBE-A79E-FE5603D3D6BF', '450000', '广西壮族自治区', '广西壮族自治区', 1, NULL, '450000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('CD877174-03CA-4C21-8BD0-FE64E33CA8B5', '350203', '思明区', '福建省厦门市思明区', 3, '350200', '350000', '350200', 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('BB19B8CA-12D3-47C9-8716-FEA9C9450DAA', '220000', '吉林省', '吉林省', 1, NULL, '220000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0),
('467B5981-833F-4A70-B482-FF0F7048B8B8', '340000', '安徽省', '安徽省', 1, NULL, '340000', NULL, 0, 1, '2026-10-31 16:03:25.2795807', 'seed-test-data', '2026-10-31 16:03:25.2795807', 'seed-test-data', 0);
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CrmBusinessActivities");

            migrationBuilder.DropTable(
                name: "CrmBusinessEntityAttributes");

            migrationBuilder.DropTable(
                name: "CrmFollowRecords");

            migrationBuilder.DropTable(
                name: "CrmMatchCandidates");

            migrationBuilder.DropTable(
                name: "CrmTransferRecords");

            migrationBuilder.DropTable(
                name: "MallAddresses");

            migrationBuilder.DropTable(
                name: "MallArticles");

            migrationBuilder.DropTable(
                name: "MallBanners");

            migrationBuilder.DropTable(
                name: "MallCampaignCoupons");

            migrationBuilder.DropTable(
                name: "MallCampaignProducts");

            migrationBuilder.DropTable(
                name: "MallCartItems");

            migrationBuilder.DropTable(
                name: "MallFreightRegions");

            migrationBuilder.DropTable(
                name: "MallMemberCoupons");

            migrationBuilder.DropTable(
                name: "MallMemberLevels");

            migrationBuilder.DropTable(
                name: "MallMemberPoints");

            migrationBuilder.DropTable(
                name: "MallNotices");

            migrationBuilder.DropTable(
                name: "MallOrderItems");

            migrationBuilder.DropTable(
                name: "MallPages");

            migrationBuilder.DropTable(
                name: "MallProductReviews");

            migrationBuilder.DropTable(
                name: "MallProductTags");

            migrationBuilder.DropTable(
                name: "MallPromotions");

            migrationBuilder.DropTable(
                name: "MallPromotionScopes");

            migrationBuilder.DropTable(
                name: "MallRefunds");

            migrationBuilder.DropTable(
                name: "MallReturns");

            migrationBuilder.DropTable(
                name: "MallShipments");

            migrationBuilder.DropTable(
                name: "MallSkus");

            migrationBuilder.DropTable(
                name: "MallStockLogs");

            migrationBuilder.DropTable(
                name: "MallTags");

            migrationBuilder.DropTable(
                name: "SystemChinaRegions");

            migrationBuilder.DropTable(
                name: "SystemDataDictionaries");

            migrationBuilder.DropTable(
                name: "SystemErrorLogs");

            migrationBuilder.DropTable(
                name: "SystemOperationLogs");

            migrationBuilder.DropTable(
                name: "SystemPermissions");

            migrationBuilder.DropTable(
                name: "SystemRolePermissions");

            migrationBuilder.DropTable(
                name: "SystemRoles");

            migrationBuilder.DropTable(
                name: "SystemUserRoles");

            migrationBuilder.DropTable(
                name: "SystemUsers");

            migrationBuilder.DropTable(
                name: "CrmHerbBaseSupplies");

            migrationBuilder.DropTable(
                name: "CrmMatchOpportunities");

            migrationBuilder.DropTable(
                name: "MallCoupons");

            migrationBuilder.DropTable(
                name: "MallCampaigns");

            migrationBuilder.DropTable(
                name: "MallPayments");

            migrationBuilder.DropTable(
                name: "MallProducts");

            migrationBuilder.DropTable(
                name: "CrmHerbBases");

            migrationBuilder.DropTable(
                name: "CrmVendorDemandItems");

            migrationBuilder.DropTable(
                name: "MallOrders");

            migrationBuilder.DropTable(
                name: "MallCategories");

            migrationBuilder.DropTable(
                name: "MallStores");

            migrationBuilder.DropTable(
                name: "CrmHerbBaseSubjects");

            migrationBuilder.DropTable(
                name: "CrmVendorDemands");

            migrationBuilder.DropTable(
                name: "MallMembers");

            migrationBuilder.DropTable(
                name: "MallFreightTemplates");

            migrationBuilder.DropTable(
                name: "CrmContacts");

            migrationBuilder.DropTable(
                name: "CrmVendors");
        }
    }
}
