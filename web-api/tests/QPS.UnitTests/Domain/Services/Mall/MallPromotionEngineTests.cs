using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;
using Xunit;

namespace QPS.UnitTests.Domain.Services.Mall;

public class MallPromotionEngineTests
{
    // 规则 v2：满减订单级（门槛按覆盖范围内行级折扣后小计）、折扣行级逐行择优、买赠 0 元直接生效。
    // 计算顺序：原价 → 行级折扣 → 买赠 → 整单满减 → 券。

    [Fact]
    public void Apply_RateThenUnmetAmountThreshold_OnlyRateTakesEffect()
    {
        // 520 全场 8 折 → 416；满 500 减 80 的门槛按折后 416 判断，不满足 → 416
        var promotion = GlobalRatePromotion(0.8m);
        var amountPromotion = GlobalAmountPromotion(500m, 80m);
        var context = SingleRowContext(520m, 1);

        var result = ApplyEngine(context, [promotion, amountPromotion]);

        Assert.Equal(416m, Total(result));
        Assert.DoesNotContain(amountPromotion.Id, result.Items.SelectMany(item => item.AppliedPromotions).Select(record => record.PromotionId));
    }

    [Fact]
    public void Apply_RateThenMetAmountThreshold_SubtractsAfterDiscount()
    {
        // 520 全场 8 折 → 416；满 300 减 50 门槛按折后 416 判断达标 → 366
        var promotion = GlobalRatePromotion(0.8m);
        var amountPromotion = GlobalAmountPromotion(300m, 50m);
        var context = SingleRowContext(520m, 1);

        var result = ApplyEngine(context, [promotion, amountPromotion]);

        Assert.Equal(366m, Total(result));
    }

    [Fact]
    public void Apply_TwoAmountPromotions_OnlyLargestTakesEffect()
    {
        // 520 同时满足满 300 减 50 与满 500 减 80，只生效抵扣更大的 80 → 440
        var small = GlobalAmountPromotion(300m, 50m);
        var large = GlobalAmountPromotion(500m, 80m);

        var result = ApplyEngine(SingleRowContext(520m, 1), [small, large]);

        Assert.Equal(440m, Total(result));
        Assert.Equal(
            [large.Id],
            result.Items.SelectMany(item => item.AppliedPromotions).Select(record => record.PromotionId).ToArray());
    }

    [Fact]
    public void Apply_PerRowBestRate_IsOrderIndependent()
    {
        // A/B 各 260：全场 8 折（各省 52）与仅覆盖 B 的分类 5 折（省 130）逐行择优
        // → A 208 + B 130 = 338，与促销数组顺序无关
        var categoryId = Guid.NewGuid();
        var mainCategoryId = Guid.NewGuid();
        var globalRate = GlobalRatePromotion(0.8m);
        var categoryRate = CategoryRatePromotion(categoryId, 0.5m);

        var rows = () => new List<SkuOrderRow>
        {
            NewRow(mainCategoryId, 260m, 1, "A"),
            NewRow(categoryId, 260m, 1, "B")
        };

        var forward = ApplyEngine(
            new OrderContext { Items = rows() },
            [globalRate, categoryRate]);
        var reversed = ApplyEngine(
            new OrderContext { Items = rows() },
            [categoryRate, globalRate]);

        Assert.Equal(338m, Total(forward));
        Assert.Equal(338m, Total(reversed));
    }

    [Fact]
    public void Apply_ProductRateAndCategoryAmount_UsesOrderLevelThresholdOnDiscountedAmount()
    {
        // 200 单品：商品级 8 折 → 160；分类级满 200 减 50 属订单级，门槛按 160 判断不满足 → 160
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var productRate = ProductRatePromotion(productId, 0.8m);
        var categoryAmount = CategoryAmountPromotion(categoryId, 200m, 50m);
        var context = new OrderContext
        {
            Items = [NewRow(categoryId, 200m, 1, "单品", productId)]
        };

        var result = ApplyEngine(context, [productRate, categoryAmount]);

        Assert.Equal(160m, Total(result));
    }

    [Fact]
    public void Apply_TwoRatePromotionsOnSameRow_OnlyBestTakesEffect()
    {
        // 同一行 8 折与 5 折：只生效优惠更大的 5 折
        var eight = GlobalRatePromotion(0.8m);
        var five = GlobalRatePromotion(0.5m);

        var result = ApplyEngine(SingleRowContext(520m, 1), [eight, five]);

        Assert.Equal(260m, Total(result));
        Assert.Equal(
            [five.Id],
            result.Items.SelectMany(item => item.AppliedPromotions).Select(record => record.PromotionId).ToArray());
    }

    [Fact]
    public void Apply_GiftStacksWithRate_AmountUnchanged()
    {
        // 40 元订单同时命中买赠与 8 折：折扣生效、赠品生效，金额只受折扣影响 → 32
        var productId = Guid.NewGuid();
        var giftSkuId = Guid.NewGuid();
        var gift = MallPromotion.Create(
            "买赠",
            string.Empty,
            MallPromotionType.Gift,
            0m,
            "Gift",
            $$"""{"skuId":"{{giftSkuId}}","quantity":1}""",
            "Product",
            null,
            null,
            null,
            true);
        var rate = GlobalRatePromotion(0.8m);
        var context = new OrderContext
        {
            Items = [NewRow(Guid.NewGuid(), 40m, 1, "参与商品", productId)]
        };
        var scopeMap = new Dictionary<Guid, List<MallPromotionScope>>
        {
            [gift.Id] = [MallPromotionScope.Create(gift.Id, "Product", productId)]
        };

        var matched = CreateEngine().Match([gift, rate.Promotion], scopeMap, context);
        var result = CreateEngine().Apply(context.Items, matched);

        Assert.Equal(32m, Total(result));
        Assert.Equal(giftSkuId, Assert.Single(result.GiftSkus).SkuId);
        Assert.Equal(gift.Id, Assert.Single(result.GiftSkus).PromotionId);
    }

    [Fact]
    public void Apply_ScopedAmountPromotion_JudgesThresholdOnScopedSubtotal()
    {
        // A 分类 300 + B 分类 200；满 250 减 40 仅覆盖 A：A 达标减 40，B 不参与 → 460
        var categoryA = Guid.NewGuid();
        var categoryB = Guid.NewGuid();
        var promotion = CategoryAmountPromotion(categoryA, 250m, 40m);
        var context = new OrderContext
        {
            Items =
            [
                NewRow(categoryA, 300m, 1, "A"),
                NewRow(categoryB, 200m, 1, "B")
            ]
        };

        var result = ApplyEngine(context, [promotion]);

        Assert.Equal(460m, Total(result));
        Assert.Equal(260m, result.Items.Single(item => item.SkuId == context.Items[0].SkuId).CurrentPrice);
        Assert.Equal(200m, result.Items.Single(item => item.SkuId == context.Items[1].SkuId).CurrentPrice);
    }

    [Fact]
    public void Apply_ScopedAmountPromotion_NotHitWhenScopeHasNoItems()
    {
        // 订单只有 B 分类 200，满 250 减 40 限定 A 分类 → 范围内无商品，不命中 → 200
        var categoryA = Guid.NewGuid();
        var categoryB = Guid.NewGuid();
        var promotion = CategoryAmountPromotion(categoryA, 250m, 40m);
        var context = new OrderContext
        {
            Items = [NewRow(categoryB, 200m, 1, "B")]
        };

        var result = ApplyEngine(context, [promotion]);

        Assert.Equal(200m, Total(result));
        Assert.All(result.Items, item => Assert.Empty(item.AppliedPromotions));
    }

    [Fact]
    public void Apply_GlobalAmountThreshold_DependsOnParticipatingItemsOnly()
    {
        // 订单 520，其中 100 元商品不在满 500 减 80 的覆盖范围 → 参与金额仅 420，不命中 → 520
        var categoryInScope = Guid.NewGuid();
        var categoryOutOfScope = Guid.NewGuid();
        var promotion = CategoryAmountPromotion(categoryInScope, 500m, 80m);
        var context = new OrderContext
        {
            Items =
            [
                NewRow(categoryInScope, 420m, 1, "参与"),
                NewRow(categoryOutOfScope, 100m, 1, "不参与")
            ]
        };

        var result = ApplyEngine(context, [promotion]);

        Assert.Equal(520m, Total(result));
        Assert.All(result.Items, item => Assert.Empty(item.AppliedPromotions));
    }

    [Fact]
    public void Apply_RecordsEveryPromotionsHittingTheSameRow()
    {
        // 520 全场 8 折 + 满 300 减 50：该行同时记录两条促销与各自优惠额（104 + 50）
        var rate = GlobalRatePromotion(0.8m);
        var amount = GlobalAmountPromotion(300m, 50m);

        var result = ApplyEngine(SingleRowContext(520m, 1), [rate, amount]);

        var row = Assert.Single(result.Items);
        Assert.Equal(2, row.AppliedPromotions.Count);
        Assert.Equal(104m, row.AppliedPromotions.Single(item => item.PromotionId == rate.Id).DiscountAmount);
        Assert.Equal(50m, row.AppliedPromotions.Single(item => item.PromotionId == amount.Id).DiscountAmount);
        Assert.Equal(366m, Total(result));

        // 分项金额按促销动作类型汇总，且与合计一致
        Assert.Equal(104m, result.RateDiscountAmount);
        Assert.Equal(50m, result.AmountDiscountAmount);
        Assert.Equal(154m, result.PromotionDiscountAmount);
        Assert.Equal(result.PromotionDiscountAmount, result.RateDiscountAmount + result.AmountDiscountAmount);
    }

    [Fact]
    public void Apply_DisjointScopedAmountPromotions_BothTakeEffect()
    {
        // A 分类 300 + B 分类 200；满 300 减 50 限定 A、满 200 减 30 限定 B。
        // 覆盖商品完全不重叠 → 两条各自生效 → 减 80
        var categoryA = Guid.NewGuid();
        var categoryB = Guid.NewGuid();
        var amountA = CategoryAmountPromotion(categoryA, 300m, 50m);
        var amountB = CategoryAmountPromotion(categoryB, 200m, 30m);
        var context = new OrderContext
        {
            Items =
            [
                NewRow(categoryA, 300m, 1, "A"),
                NewRow(categoryB, 200m, 1, "B")
            ]
        };

        var result = ApplyEngine(context, [amountA, amountB]);

        Assert.Equal(420m, Total(result));
        var records = result.Items.SelectMany(item => item.AppliedPromotions).ToList();
        Assert.Equal(2, records.Count);
        Assert.Equal(50m, records.Single(record => record.PromotionId == amountA.Id).DiscountAmount);
        Assert.Equal(30m, records.Single(record => record.PromotionId == amountB.Id).DiscountAmount);
    }

    [Fact]
    public void Apply_PartiallyOverlappingAmountPromotions_OnlyLargestTakesEffect()
    {
        // 满 400 减 100 覆盖 A+B，满 150 减 50 只覆盖 B；B 上有交集 → 只取 100
        var categoryA = Guid.NewGuid();
        var categoryB = Guid.NewGuid();
        var wide = MultiCategoryAmountPromotion(400m, 100m, categoryA, categoryB);
        var narrow = CategoryAmountPromotion(categoryB, 150m, 50m);
        var context = new OrderContext
        {
            Items =
            [
                NewRow(categoryA, 300m, 1, "A"),
                NewRow(categoryB, 200m, 1, "B")
            ]
        };

        var result = ApplyEngine(context, [wide, narrow]);

        Assert.Equal(400m, Total(result));
        var records = result.Items.SelectMany(item => item.AppliedPromotions).ToList();
        // 覆盖 A+B 的满减按行分摊，每个命中行各记一条；被互斥掉的 B 专项满减不应出现
        Assert.Equal([wide.Id], records.Select(record => record.PromotionId).Distinct().ToArray());
    }

    [Fact]
    public void Apply_AmountAllocation_RoundsToCentsAndLastRowAbsorbsRemainder()
    {
        // 3 行各 100，满 300 减 50：按占比分摊到分，尾差补最后一行 → Σ行优惠 == 50
        var promotion = GlobalAmountPromotion(300m, 50m);
        var context = new OrderContext
        {
            Items =
            [
                NewRow(Guid.NewGuid(), 100m, 1, "1"),
                NewRow(Guid.NewGuid(), 100m, 1, "2"),
                NewRow(Guid.NewGuid(), 100m, 1, "3")
            ]
        };

        var result = ApplyEngine(context, [promotion]);

        var records = result.Items.Select(item => item.AppliedPromotions.Single()).ToList();
        Assert.Equal([16.67m, 16.66m, 16.67m], records.Select(record => record.DiscountAmount).ToArray());
        Assert.Equal(50m, records.Sum(record => record.DiscountAmount));
        Assert.Equal(250m, Total(result));
    }

    [Fact]
    public void Match_RateThreshold_StillGatesSettlement_UnlessDisplayFlagIsUsed()
    {
        var promotion = MallPromotion.Create(
            "满100打8折",
            string.Empty,
            MallPromotionType.DiscountRate,
            100m,
            "DiscountRate",
            """{"rate":0.8}""",
            "Global",
            null,
            null,
            null,
            true);
        var context = new OrderContext
        {
            Items = [NewRow(Guid.NewGuid(), 50m, 1, "未达门槛商品")]
        };
        var engine = CreateEngine();

        // 结算/预览口径：单行 50 < 门槛 100，不命中
        Assert.Empty(engine.Match([promotion], new Dictionary<Guid, List<MallPromotionScope>>(), context));

        // 商品列表展示口径：忽略订单累计门槛，按折扣率给出到手价
        var matched = engine.Match(
            [promotion],
            new Dictionary<Guid, List<MallPromotionScope>>(),
            context,
            ignoreRateThreshold: true);
        var result = engine.Apply(context.Items, matched);

        Assert.Single(matched);
        Assert.Equal(40m, result.Items[0].CurrentPrice);
    }

    [Fact]
    public void Match_CategoryScopeIncludesProductsInChildCategories()
    {
        var parentCategoryId = Guid.NewGuid();
        var childCategoryId = Guid.NewGuid();
        var promotion = MallPromotion.Create(
            "父级分类折扣",
            string.Empty,
            MallPromotionType.DiscountRate,
            1m,
            "DiscountRate",
            """{"rate":0.5}""",
            "Category",
            null,
            null,
            null,
            true);
        var scopeMap = new Dictionary<Guid, List<MallPromotionScope>>
        {
            [promotion.Id] = [MallPromotionScope.Create(promotion.Id, "Category", parentCategoryId)]
        };
        var context = new OrderContext
        {
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

        var matched = CreateEngine().Match([promotion], scopeMap, context);

        Assert.Single(matched);
        Assert.Single(matched[0].AffectedItems);
    }

    [Fact]
    public void GiftPromotion_UsesScopeAsPurchaseCondition()
    {
        var scopeProductId = Guid.NewGuid();
        var otherProductId = Guid.NewGuid();
        var giftSkuId = Guid.NewGuid();
        var promotion = MallPromotion.Create(
            "买二赠一",
            string.Empty,
            MallPromotionType.Gift,
            0m,
            "Gift",
            $$"""{"skuId":"{{giftSkuId}}","quantity":1}""",
            "Product",
            null,
            null,
            null,
            true);
        var scopeMap = new Dictionary<Guid, List<MallPromotionScope>>
        {
            [promotion.Id] = [MallPromotionScope.Create(promotion.Id, "Product", scopeProductId)]
        };
        var context = new OrderContext
        {
            Items =
            [
                NewRow(Guid.NewGuid(), 0m, 1, "参与", scopeProductId),
                NewRow(Guid.NewGuid(), 0m, 10, "不参与", otherProductId)
            ]
        };
        var engine = CreateEngine();

        var matched = engine.Match([promotion], scopeMap, context);
        var result = engine.Apply(context.Items, matched);

        Assert.Single(matched);
        Assert.Equal(scopeProductId, Assert.Single(matched[0].AffectedItems).ProductId);
        Assert.Equal(giftSkuId, Assert.Single(result.GiftSkus).SkuId);

        context.Items.RemoveAll(item => item.ProductId == scopeProductId);
        Assert.Empty(engine.Match([promotion], scopeMap, context));
    }

    [Fact]
    public void GiftPromotion_RequiresScopedAmountToReachThreshold()
    {
        var targetProductId = Guid.NewGuid();
        var otherProductId = Guid.NewGuid();
        var giftSkuId = Guid.NewGuid();
        var promotion = MallPromotion.Create(
            "指定商品满额买赠",
            string.Empty,
            MallPromotionType.Gift,
            40m,
            "Gift",
            $$"""{"skuId":"{{giftSkuId}}","quantity":1}""",
            "Product",
            null,
            null,
            null,
            true);
        var scopeMap = new Dictionary<Guid, List<MallPromotionScope>>
        {
            [promotion.Id] = [MallPromotionScope.Create(promotion.Id, "Product", targetProductId)]
        };
        var context = new OrderContext
        {
            Items =
            [
                NewRow(Guid.NewGuid(), 20m, 1, "目标商品", targetProductId),
                NewRow(Guid.NewGuid(), 10m, 10, "其他商品", otherProductId)
            ]
        };
        var engine = CreateEngine();

        // 目标商品金额未达门槛（20 < 40）→ 不触发
        Assert.Empty(engine.Match([promotion], scopeMap, context));

        context.Items[0].Quantity = 2;
        var matched = engine.Match([promotion], scopeMap, context);
        var result = engine.Apply(context.Items, matched);

        Assert.Single(matched);
        Assert.Equal(giftSkuId, Assert.Single(result.GiftSkus).SkuId);
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

    private static ApplyResult ApplyEngine(OrderContext context, params PromotionSetup[] setups)
    {
        var promotions = setups.Select(setup => setup.Promotion).ToList();
        var scopeMap = setups
            .Where(setup => setup.Scopes.Count > 0)
            .ToDictionary(setup => setup.Promotion.Id, setup => setup.Scopes);
        var engine = CreateEngine();
        var matched = engine.Match(promotions, scopeMap, context);
        return engine.Apply(context.Items, matched);
    }

    private static decimal Total(ApplyResult result)
    {
        return result.Items.Sum(item => item.CurrentPrice * item.Quantity);
    }

    private static PromotionSetup GlobalRatePromotion(decimal rate)
    {
        return new PromotionSetup(
            MallPromotion.Create(
                $"全场折扣 {rate}",
                string.Empty,
                MallPromotionType.DiscountRate,
                0m,
                "DiscountRate",
                $$"""{"rate":{{rate}}}""",
                "Global",
                null,
                null,
                null,
                true),
            []);
    }

    private static PromotionSetup GlobalAmountPromotion(decimal threshold, decimal amount)
    {
        return new PromotionSetup(
            MallPromotion.Create(
                $"全场满 {threshold} 减 {amount}",
                string.Empty,
                MallPromotionType.DiscountAmount,
                threshold,
                "DiscountAmount",
                $$"""{"amount":{{amount}}}""",
                "Global",
                null,
                null,
                null,
                true),
            []);
    }

    private static PromotionSetup ProductRatePromotion(Guid productId, decimal rate)
    {
        var promotion = MallPromotion.Create(
            $"商品折扣 {rate}",
            string.Empty,
            MallPromotionType.DiscountRate,
            0m,
            "DiscountRate",
            $$"""{"rate":{{rate}}}""",
            "Product",
            null,
            null,
            null,
            true);
        return new PromotionSetup(
            promotion,
            [MallPromotionScope.Create(promotion.Id, "Product", productId)]);
    }

    private static PromotionSetup CategoryRatePromotion(Guid categoryId, decimal rate)
    {
        var promotion = MallPromotion.Create(
            $"分类折扣 {rate}",
            string.Empty,
            MallPromotionType.DiscountRate,
            0m,
            "DiscountRate",
            $$"""{"rate":{{rate}}}""",
            "Category",
            null,
            null,
            null,
            true);
        return new PromotionSetup(
            promotion,
            [MallPromotionScope.Create(promotion.Id, "Category", categoryId)]);
    }

    private static PromotionSetup CategoryAmountPromotion(Guid categoryId, decimal threshold, decimal amount)
    {
        return MultiCategoryAmountPromotion(threshold, amount, categoryId);
    }

    private static PromotionSetup MultiCategoryAmountPromotion(
        decimal threshold,
        decimal amount,
        params Guid[] categoryIds)
    {
        var promotion = MallPromotion.Create(
            $"分类满 {threshold} 减 {amount}",
            string.Empty,
            MallPromotionType.DiscountAmount,
            threshold,
            "DiscountAmount",
            $$"""{"amount":{{amount}}}""",
            "Category",
            null,
            null,
            null,
            true);
        return new PromotionSetup(
            promotion,
            categoryIds
                .Select(categoryId => MallPromotionScope.Create(promotion.Id, "Category", categoryId))
                .ToList());
    }

    private static SkuOrderRow NewRow(
        Guid categoryId,
        decimal price,
        int quantity,
        string name,
        Guid? productId = null)
    {
        return new SkuOrderRow
        {
            SkuId = Guid.NewGuid(),
            ProductId = productId ?? Guid.NewGuid(),
            CategoryId = categoryId,
            CategoryIds = [categoryId],
            OriginalPrice = price,
            CurrentPrice = price,
            Quantity = quantity
        };
    }

    private static OrderContext SingleRowContext(decimal price, int quantity)
    {
        return new OrderContext { Items = [NewRow(Guid.NewGuid(), price, quantity, "单品")] };
    }

    /// <summary>促销与其作用范围绑定的测试装配单元。</summary>
    private sealed record PromotionSetup(MallPromotion Promotion, List<MallPromotionScope> Scopes)
    {
        public Guid Id => Promotion.Id;
    }
}
