using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using Xunit;

namespace QPS.UnitTests.Domain.Services.Mall;

public class MallPromotionEngineTests
{
    [Fact]
    public void Apply_DiscountsOnlyItemsMatchingPromotionScope()
    {
        var targetCategoryId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();
        var targetProductId = Guid.NewGuid();
        var otherProductId = Guid.NewGuid();
        var targetSkuId = Guid.NewGuid();
        var otherSkuId = Guid.NewGuid();
        var promotion = MallPromotion.Create(
            "指定分类满减",
            string.Empty,
            "FullAmount",
            """{"minAmount":20}""",
            "DiscountAmount",
            """{"amount":10}""",
            "Category",
            1,
            false,
            0,
            0,
            null,
            null,
            true);
        promotion.AddScope(MallPromotionScope.Create(promotion.Id, "Category", targetCategoryId));

        var engine = new MallPromotionEngine(
            new TriggerStrategyRegistry(new ITriggerStrategy[] { new FullAmountTrigger() }),
            new ActionStrategyRegistry(new IActionStrategy[] { new DiscountAmountAction() }));
        var context = new OrderContext
        {
            GoodsAmount = 50,
            Items =
            [
                new SkuOrderRow
                {
                    SkuId = targetSkuId,
                    ProductId = targetProductId,
                    CategoryId = targetCategoryId,
                    OriginalPrice = 20,
                    CurrentPrice = 20,
                    Quantity = 1
                },
                new SkuOrderRow
                {
                    SkuId = otherSkuId,
                    ProductId = otherProductId,
                    CategoryId = otherCategoryId,
                    OriginalPrice = 30,
                    CurrentPrice = 30,
                    Quantity = 1
                }
            ]
        };

        var matched = engine.Match([promotion], context);
        var result = engine.Apply(context.Items, matched);

        Assert.Equal(10, result.Items.Single(item => item.SkuId == targetSkuId).CurrentPrice);
        Assert.Equal(30, result.Items.Single(item => item.SkuId == otherSkuId).CurrentPrice);
        Assert.Equal(promotion.Id, result.Items.Single(item => item.SkuId == targetSkuId).AppliedPromotionId);
        Assert.Null(result.Items.Single(item => item.SkuId == otherSkuId).AppliedPromotionId);
    }

    [Fact]
    public void Match_FullAmountTriggerUsesOnlyItemsWithinPromotionScope()
    {
        var targetCategoryId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();
        var promotion = MallPromotion.Create(
            "指定分类满减",
            string.Empty,
            "FullAmount",
            """{"minAmount":50}""",
            "DiscountAmount",
            """{"amount":10}""",
            "Category",
            1,
            false,
            0,
            0,
            null,
            null,
            true);
        promotion.AddScope(MallPromotionScope.Create(promotion.Id, "Category", targetCategoryId));

        var engine = new MallPromotionEngine(
            new TriggerStrategyRegistry(new ITriggerStrategy[] { new FullAmountTrigger() }),
            new ActionStrategyRegistry(new IActionStrategy[] { new DiscountAmountAction() }));
        var context = new OrderContext
        {
            GoodsAmount = 100,
            Items =
            [
                new SkuOrderRow
                {
                    ProductId = Guid.NewGuid(),
                    CategoryId = targetCategoryId,
                    OriginalPrice = 20,
                    CurrentPrice = 20,
                    Quantity = 1
                },
                new SkuOrderRow
                {
                    ProductId = Guid.NewGuid(),
                    CategoryId = otherCategoryId,
                    OriginalPrice = 80,
                    CurrentPrice = 80,
                    Quantity = 1
                }
            ]
        };

        Assert.Empty(engine.Match([promotion], context));
    }

    [Fact]
    public void Match_CategoryScopeIncludesProductsInChildCategories()
    {
        var parentCategoryId = Guid.NewGuid();
        var childCategoryId = Guid.NewGuid();
        var promotion = MallPromotion.Create(
            "父级分类折扣",
            string.Empty,
            "FullAmount",
            """{"minAmount":1}""",
            "DiscountRate",
            """{"rate":0.5}""",
            "Category",
            1,
            false,
            0,
            0,
            null,
            null,
            true);
        promotion.AddScope(MallPromotionScope.Create(promotion.Id, "Category", parentCategoryId));

        var engine = new MallPromotionEngine(
            new TriggerStrategyRegistry(new ITriggerStrategy[] { new FullAmountTrigger() }),
            new ActionStrategyRegistry(new IActionStrategy[] { new DiscountRateAction() }));
        var context = new OrderContext
        {
            GoodsAmount = 20,
            Items =
            [
                new SkuOrderRow
                {
                    ProductId = Guid.NewGuid(),
                    CategoryId = childCategoryId,
                    CategoryIds = [childCategoryId, parentCategoryId],
                    OriginalPrice = 20,
                    CurrentPrice = 20,
                    Quantity = 1
                }
            ]
        };

        var matched = engine.Match([promotion], context);

        Assert.Single(matched);
        Assert.Single(matched[0].AffectedItems);
    }

    [Fact]
    public void BuyGiftPromotion_UsesPurchaseProductAsTriggerAndIndependentScope()
    {
        var buyProductId = Guid.NewGuid();
        var scopeProductId = Guid.NewGuid();
        var otherProductId = Guid.NewGuid();
        var giftSkuId = Guid.NewGuid();
        var promotion = MallPromotion.Create(
            "买二赠一",
            string.Empty,
            "BuyProduct",
            $$"""{"productIds":["{{buyProductId}}"],"minQuantity":2}""",
            "Gift",
            $$"""{"skuId":"{{giftSkuId}}","quantity":1}""",
            "Product",
            1,
            false,
            0,
            0,
            null,
            null,
            true);
        promotion.AddScope(MallPromotionScope.Create(promotion.Id, "Product", scopeProductId));

        var engine = new MallPromotionEngine(
            new TriggerStrategyRegistry(new ITriggerStrategy[] { new BuyProductTrigger() }),
            new ActionStrategyRegistry(new IActionStrategy[] { new GiftAction() }));
        var context = new OrderContext
        {
            Items =
            [
                new SkuOrderRow { ProductId = buyProductId, Quantity = 2 },
                new SkuOrderRow { ProductId = scopeProductId, Quantity = 1 },
                new SkuOrderRow { ProductId = otherProductId, Quantity = 10 }
            ]
        };

        var matched = engine.Match([promotion], context);
        var result = engine.Apply(context.Items, matched);

        Assert.Single(matched);
        Assert.Equal(scopeProductId, Assert.Single(matched[0].AffectedItems).ProductId);
        Assert.Equal(giftSkuId, Assert.Single(result.GiftSkus).SkuId);

        context.Items.RemoveAll(item => item.ProductId == scopeProductId);
        Assert.Empty(engine.Match([promotion], context));
    }

    [Fact]
    public void BuyProductTrigger_RequiresConfiguredMinimumQuantity()
    {
        var productId = Guid.NewGuid();
        var trigger = new BuyProductTrigger();
        using var config = global::System.Text.Json.JsonDocument.Parse(
            $$"""{"productIds":["{{productId}}"],"minQuantity":2}""");
        var context = new OrderContext
        {
            Items =
            [
                new SkuOrderRow { ProductId = productId, Quantity = 1 },
                new SkuOrderRow { ProductId = productId, Quantity = 1 }
            ]
        };

        Assert.True(trigger.IsMatch(context, config.RootElement));
        context.Items[1].ProductId = Guid.NewGuid();
        Assert.False(trigger.IsMatch(context, config.RootElement));
    }
}
