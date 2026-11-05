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
        var quantityBySku = inputs
            .GroupBy(input => input.Sku.Id)
            .ToDictionary(group => group.Key, group => group.Sum(input => input.Quantity));

        var activePromotions = await _dbContext.MallPromotions
            .AsNoTracking()
            .WhereEffective(DateTime.Now)
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

        var categoryParents = await LoadCategoryParentsAsync(
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
                        categoryParents);
                })
                .ToList()
        };

        if (activePromotions.Count > 0)
        {
            var usages = await _dbContext.MallPromotionUsages
                .AsNoTracking()
                .Where(usage => promotionIds.Contains(usage.PromotionId) && usage.UserId == memberId)
                .ToListAsync(cancellationToken);
            orderContext.PromotionUsageMap = usages.ToDictionary(
                usage => usage.PromotionId,
                usage => usage.UsageCount);
        }

        return new MallPromotionContextData
        {
            ActivePromotions = activePromotions,
            ScopeMap = scopeMap,
            OrderContext = orderContext
        };
    }

    private async Task<Dictionary<Guid, Guid?>> LoadCategoryParentsAsync(
        IEnumerable<MallProduct> products,
        CancellationToken cancellationToken)
    {
        var categoryIds = products
            .Where(product => product.CategoryId.HasValue)
            .Select(product => product.CategoryId!.Value)
            .Distinct()
            .ToArray();
        if (categoryIds.Length == 0)
        {
            return new Dictionary<Guid, Guid?>();
        }

        return await _dbContext.MallCategories
            .Where(category => categoryIds.Contains(category.Id))
            .Select(category => new { category.Id, category.ParentId })
            .ToDictionaryAsync(
                category => category.Id,
                category => category.ParentId,
                cancellationToken);
    }

    private static SkuOrderRow BuildSkuOrderRow(
        MallSku sku,
        MallProduct product,
        int quantity,
        IReadOnlyDictionary<Guid, Guid?> categoryParents)
    {
        var categoryIds = new List<Guid>();
        if (product.CategoryId.HasValue)
        {
            var categoryId = product.CategoryId.Value;
            categoryIds.Add(categoryId);
            if (categoryParents.TryGetValue(categoryId, out var parentId) && parentId.HasValue)
            {
                categoryIds.Add(parentId.Value);
            }
        }

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
