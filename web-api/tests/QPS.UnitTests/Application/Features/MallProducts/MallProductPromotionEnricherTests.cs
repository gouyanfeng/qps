using QPS.Mall.Application.Features.MallProducts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using QPS.UnitTests.Common;
using Xunit;

namespace QPS.UnitTests.Application.Features.MallProducts;

/// <summary>
/// 商品展示角标与折后价必须与下单共用 MallPromotionEngine.AppliesToSku，
/// 覆盖 Global/Category（含祖先链）/Product/Sku 四种作用域，避免出现第二套判定口径。
/// </summary>
public class MallProductPromotionEnricherTests
{
    [Fact]
    public async Task CategoryScope_BadgesChildCategoryProducts_AndLeavesOtherCategoriesEmpty()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;

        var rootCategory = MallCategory.Create("ROOT", "根茎类", null, null, 0, true);
        var childCategory = MallCategory.Create("CHILD", "人参", rootCategory.Id, null, 0, true);
        var otherCategory = MallCategory.Create("OTHER", "花叶类", null, null, 0, true);
        var tag = MallTag.Create("PROMO_DISCOUNT", "限时满减", "#C2410C", 0, true, MallTagType.Promotion);
        db.MallCategories.AddRange(rootCategory, childCategory, otherCategory);
        db.MallTags.Add(tag);

        var promotion = MallPromotion.Create(
            "分类满减", string.Empty, MallPromotionType.DiscountAmount, 100m,
            "DiscountAmount", """{"amount":15}""", "Category", tag.Id, null, null, true);
        db.MallPromotions.Add(promotion);
        db.MallPromotionScopes.Add(MallPromotionScope.Create(promotion.Id, "Category", rootCategory.Id));
        await db.SaveChangesAsync();

        var (childProduct, _) = CreateProduct(childCategory.Id, 100m);
        var (otherProduct, _) = CreateProduct(otherCategory.Id, 100m);

        await new MallProductPromotionEnricher(db, CreateEngine()).EnrichAsync([childProduct, otherProduct], CancellationToken.None);

        Assert.Equal("限时满减", childProduct.PromotionBadgeText);
        Assert.Equal("#C2410C", childProduct.PromotionBadgeColor);
        Assert.Contains(childProduct.Promotions, item => item.Id == promotion.Id);
        Assert.Equal(string.Empty, otherProduct.PromotionBadgeText);
        Assert.Empty(otherProduct.Promotions);
    }

    [Fact]
    public async Task AmountPromotionBelowOrderThreshold_StillShowsBadgeForScopedProduct()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;

        var category = MallCategory.Create("GINSENG", "人参", null, null, 0, true);
        var tag = MallTag.Create("PROMO_DISCOUNT", "限时满减", "#C2410C", 0, true, MallTagType.Promotion);
        db.MallCategories.Add(category);
        db.MallTags.Add(tag);

        var promotion = MallPromotion.Create(
            "分类满减", string.Empty, MallPromotionType.DiscountAmount, 100m,
            "DiscountAmount", """{"amount":10}""", "Category", tag.Id, null, null, true);
        db.MallPromotions.Add(promotion);
        db.MallPromotionScopes.Add(MallPromotionScope.Create(promotion.Id, "Category", category.Id));
        await db.SaveChangesAsync();

        // 单价 50 < 满减门槛 100：单行无法触发满减，但商品仍属于该促销范围，应展示角标
        var (product, _) = CreateProduct(category.Id, 50m);

        await new MallProductPromotionEnricher(db, CreateEngine()).EnrichAsync([product], CancellationToken.None);

        Assert.Equal("限时满减", product.PromotionBadgeText);
        Assert.Contains(product.Promotions, item => item.Id == promotion.Id);
    }

    [Fact]
    public async Task GlobalProductSkuScopes_AllBadge_WithPromotionNameFallback()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;

        var (productA, _) = CreateProduct(null, 100m);
        var (productB, _) = CreateProduct(null, 100m);
        var (productC, skuC) = CreateProduct(null, 100m);

        var globalPromotion = CreateAmountPromotion("全场满减", "Global", 5m, null);
        var productPromotion = CreateAmountPromotion("单品满减", "Product", 8m, null);
        var skuPromotion = CreateAmountPromotion("规格满减", "Sku", 6m, null);
        db.MallPromotions.AddRange(globalPromotion, productPromotion, skuPromotion);
        db.MallPromotionScopes.Add(MallPromotionScope.Create(productPromotion.Id, "Product", productB.Id));
        db.MallPromotionScopes.Add(MallPromotionScope.Create(skuPromotion.Id, "Sku", skuC));
        await db.SaveChangesAsync();

        await new MallProductPromotionEnricher(db, CreateEngine()).EnrichAsync(
            [productA, productB, productC], CancellationToken.None);

        Assert.Equal("全场满减", productA.PromotionBadgeText);
        Assert.Equal(string.Empty, productA.PromotionBadgeColor);
        Assert.Equal("单品满减", productB.PromotionBadgeText);
        Assert.Equal("规格满减", productC.PromotionBadgeText);
        Assert.Single(productA.Promotions);
        Assert.Equal(2, productB.Promotions.Count);
        Assert.Equal(2, productC.Promotions.Count);
    }

    [Fact]
    public async Task RatePromotion_SetsDiscountedPriceAndLabel_WithoutAmountBadge()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;

        var category = MallCategory.Create("CHILD", "鹿茸", null, null, 0, true);
        var tag = MallTag.Create("PROMO_NEWUSER", "8折", "#409EFF", 0, true, MallTagType.Promotion);
        db.MallCategories.Add(category);
        db.MallTags.Add(tag);

        var promotion = MallPromotion.Create(
            "打折", string.Empty, MallPromotionType.DiscountRate, 0m,
            "DiscountRate", """{"rate":0.8}""", "Category", tag.Id, null, null, true);
        db.MallPromotions.Add(promotion);
        db.MallPromotionScopes.Add(MallPromotionScope.Create(promotion.Id, "Category", category.Id));
        await db.SaveChangesAsync();

        var (product, _) = CreateProduct(category.Id, 380m, 532m);

        await new MallProductPromotionEnricher(db, CreateEngine()).EnrichAsync([product], CancellationToken.None);

        Assert.Equal(304m, product.Skus[0].DiscountedPrice);
        Assert.Equal("8折", product.Skus[0].DiscountLabel);
        Assert.Equal(string.Empty, product.PromotionBadgeText);
        Assert.Contains(product.Tags, item => item.Id == tag.Id);
    }

    [Fact]
    public async Task RatePromotion_ShowsDiscountedPriceForScopedSkuBelowOrderThreshold()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;

        var category = MallCategory.Create("CHILD", "鹿茸", null, null, 0, true);
        var tag = MallTag.Create("PROMO_NEWUSER", "8折", "#409EFF", 0, true, MallTagType.Promotion);
        db.MallCategories.Add(category);
        db.MallTags.Add(tag);

        var promotion = MallPromotion.Create(
            "满100打8折", string.Empty, MallPromotionType.DiscountRate, 100m,
            "DiscountRate", """{"rate":0.8}""", "Category", tag.Id, null, null, true);
        db.MallPromotions.Add(promotion);
        db.MallPromotionScopes.Add(MallPromotionScope.Create(promotion.Id, "Category", category.Id));
        await db.SaveChangesAsync();

        // 单价 50 < 订单门槛 100：列表展示仍应给出折扣率到手价
        var (product, _) = CreateProduct(category.Id, 50m);

        await new MallProductPromotionEnricher(db, CreateEngine()).EnrichAsync([product], CancellationToken.None);

        Assert.Equal(40m, product.Skus[0].DiscountedPrice);
        Assert.Equal("8折", product.Skus[0].DiscountLabel);
    }

    [Fact]
    public async Task MultipleAmountPromotions_PicksLargestDiscountForBadge()
    {
        await using var factory = await TestSqliteDbContextFactory.CreateAsync();
        var db = factory.Context;

        var category = MallCategory.Create("CHILD", "根茎类", null, null, 0, true);
        db.MallCategories.Add(category);

        var categoryPromotion = CreateAmountPromotion("分类满减", "Category", 10m, null);
        var globalPromotion = CreateAmountPromotion("全场满减", "Global", 20m, null);
        db.MallPromotions.AddRange(categoryPromotion, globalPromotion);
        db.MallPromotionScopes.Add(MallPromotionScope.Create(categoryPromotion.Id, "Category", category.Id));
        await db.SaveChangesAsync();

        var (product, _) = CreateProduct(category.Id, 100m);

        await new MallProductPromotionEnricher(db, CreateEngine()).EnrichAsync([product], CancellationToken.None);

        Assert.Equal("全场满减", product.PromotionBadgeText);
        Assert.Equal(2, product.Promotions.Count);
    }

    private static MallPromotionEngine CreateEngine()
    {
        return new MallPromotionEngine(new ActionStrategyRegistry(
            new IActionStrategy[]
            {
                new DiscountAmountAction(),
                new DiscountRateAction(),
                new GiftAction(),
                new FreightFreeAction()
            }));
    }

    private static MallPromotion CreateAmountPromotion(
        string name,
        string scopeType,
        decimal amount,
        Guid? tagId)
    {
        return MallPromotion.Create(
            name, string.Empty, MallPromotionType.DiscountAmount, 100m,
            "DiscountAmount", $$"""{"amount":{{amount}}}""", scopeType, tagId, null, null, true);
    }

    private static (MallProductDto Product, Guid SkuId) CreateProduct(
        Guid? categoryId,
        decimal price,
        decimal marketPrice = 0m)
    {
        var productId = Guid.NewGuid();
        var skuId = Guid.NewGuid();
        var product = new MallProductDto
        {
            Id = productId,
            CategoryId = categoryId,
            Name = "测试商品",
            Skus =
            [
                new MallSkuDto
                {
                    Id = skuId,
                    ProductId = productId,
                    SkuCode = "SKU",
                    SpecName = "默认规格",
                    Price = price,
                    MarketPrice = marketPrice,
                    Stock = 100,
                    IsActive = true
                }
            ],
            Tags = []
        };
        return (product, skuId);
    }
}
