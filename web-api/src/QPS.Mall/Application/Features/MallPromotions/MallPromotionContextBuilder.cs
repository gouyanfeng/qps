using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Domain.Entities;
using QPS.Mall.Domain.Services;

namespace QPS.Mall.Application.Features.MallPromotions;

/// <summary>
/// 促销上下文统一装配器：购物车试算、订单确认页预览与正式下单共用同一套
/// “有效促销 + 范围表 + 引擎订单上下文”装配逻辑，避免三处各自重复实现。
/// </summary>
public class MallPromotionContextBuilder
{
    private readonly IMallDbContext _dbContext;

    public MallPromotionContextBuilder(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>按传入 SKU/商品与数量装配促销计算所需的完整上下文。</summary>
    public async Task<MallPromotionContextData> BuildAsync(
        Guid memberId,
        IReadOnlyCollection<MallPromotionSkuInput> inputs,
        CancellationToken cancellationToken)
    {
        var activePromotions = await _dbContext.MallPromotions
            .AsNoTracking()
            .WhereEffective(DateTime.Now)
            .OrderBy(promotion => promotion.CreatedAt)
            .ThenBy(promotion => promotion.Id)
            .ToListAsync(cancellationToken);

        var promotionIds = activePromotions.Select(promotion => promotion.Id).ToList();
        var scopeMap = promotionIds.Count == 0
            ? new Dictionary<Guid, List<MallPromotionScope>>()
            : await _dbContext.MallPromotionScopes
                .Where(scope => promotionIds.Contains(scope.PromotionId))
                .GroupBy(scope => scope.PromotionId)
                .ToDictionaryAsync(
                    group => group.Key,
                    group => group.ToList(),
                    cancellationToken);

        var categoryAncestors = await LoadCategoryAncestorsAsync(
            inputs.Select(input => input.Product),
            cancellationToken);

        var orderContext = new OrderContext
        {
            UserId = memberId,
            Items = inputs
                .GroupBy(input => input.Sku.Id)
                .Select(group =>
                {
                    var input = group.First();
                    return BuildSkuOrderRow(
                        input.Sku,
                        input.Product,
                        group.Sum(item => item.Quantity),
                        categoryAncestors);
                })
                .ToList()
        };

        return new MallPromotionContextData
        {
            ActivePromotions = activePromotions,
            ScopeMap = scopeMap,
            OrderContext = orderContext
        };
    }

    /// <summary>按商品分类逐级加载完整祖先链（含自身），与商品页展示口径一致。</summary>
    private async Task<Dictionary<Guid, Guid[]>> LoadCategoryAncestorsAsync(
        IEnumerable<MallProduct> products,
        CancellationToken cancellationToken)
    {
        var pendingIds = products
            .Where(product => product.CategoryId.HasValue)
            .Select(product => product.CategoryId!.Value)
            .Distinct()
            .ToHashSet();
        var parentById = new Dictionary<Guid, Guid?>();

        while (pendingIds.Count > 0)
        {
            var categories = await _dbContext.MallCategories
                .AsNoTracking()
                .Where(category => pendingIds.Contains(category.Id))
                .Select(category => new { category.Id, category.ParentId })
                .ToListAsync(cancellationToken);
            pendingIds.Clear();

            foreach (var category in categories)
            {
                parentById[category.Id] = category.ParentId;
                if (category.ParentId.HasValue && !parentById.ContainsKey(category.ParentId.Value))
                {
                    pendingIds.Add(category.ParentId.Value);
                }
            }
        }

        // 购物车中同一商品的多个规格会带出多个相同 Id 的商品实例，去重后再建索引
        return products
            .DistinctBy(product => product.Id)
            .ToDictionary(
                product => product.Id,
                product =>
                {
                    var ancestors = new List<Guid>();
                    var currentId = product.CategoryId;
                    var visited = new HashSet<Guid>();
                    while (currentId.HasValue && visited.Add(currentId.Value))
                    {
                        ancestors.Add(currentId.Value);
                        currentId = parentById.GetValueOrDefault(currentId.Value);
                    }

                    return ancestors.ToArray();
                });
    }

    private static SkuOrderRow BuildSkuOrderRow(
        MallSku sku,
        MallProduct product,
        int quantity,
        IReadOnlyDictionary<Guid, Guid[]> categoryAncestors)
    {
        var categoryIds = product.CategoryId.HasValue
            ? categoryAncestors.GetValueOrDefault(product.CategoryId.Value)?.ToList() ?? new List<Guid> { product.CategoryId.Value }
            : new List<Guid>();

        return new SkuOrderRow
        {
            SkuId = sku.Id,
            ProductId = product.Id,
            CategoryId = product.CategoryId,
            CategoryIds = categoryIds,
            OriginalPrice = sku.Price,
            CurrentPrice = sku.Price,
            Quantity = quantity,
            WeightGram = sku.WeightGram
        };
    }
}

/// <summary>促销上下文装配输入：一次传入 SKU、所属商品与该 SKU 的购买数量。</summary>
public sealed record MallPromotionSkuInput(MallSku Sku, MallProduct Product, int Quantity);

/// <summary>促销上下文装配结果：有效促销、范围表与引擎订单上下文。</summary>
public sealed class MallPromotionContextData
{
    public required List<MallPromotion> ActivePromotions { get; init; }

    public required Dictionary<Guid, List<MallPromotionScope>> ScopeMap { get; init; }

    public required OrderContext OrderContext { get; init; }
}
