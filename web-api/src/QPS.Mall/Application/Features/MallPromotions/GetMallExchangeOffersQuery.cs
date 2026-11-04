using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;
using System.Text.Json;

namespace QPS.Mall.Application.Features.MallPromotions;

public class GetMallExchangeOffersQuery : IRequest<List<MallExchangeOfferDto>>
{
    /// <summary>当前全订单商品原价合计。</summary>
    public decimal Amount { get; set; }

    /// <summary>当前会员（可选），用于每人限用次数校验。</summary>
    public Guid? MemberId { get; set; }
}

public class GetMallExchangeOffersHandler : IRequestHandler<GetMallExchangeOffersQuery, List<MallExchangeOfferDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallExchangeOffersHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MallExchangeOfferDto>> Handle(GetMallExchangeOffersQuery request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return new List<MallExchangeOfferDto>();
        }

        var now = DateTime.Now;
        var promotions = await _dbContext.MallPromotions
            .AsNoTracking()
            .Where(p => p.ActionType == "TradeIn")
            .WhereEffective(_dbContext, now)
            .ToListAsync(cancellationToken);

        var result = new List<MallExchangeOfferDto>();

        foreach (var promotion in promotions)
        {
            var minAmount = promotion.ThresholdAmount;
            Guid skuId;
            decimal tradeInPrice;
            int maxQuantity;
            try
            {
                using var actionDoc = JsonDocument.Parse(promotion.ActionConfigJson);
                var actionConfig = actionDoc.RootElement;
                if (!actionConfig.TryGetProperty("skuId", out var skuElem) ||
                    !Guid.TryParse(skuElem.GetString(), out skuId) ||
                    !actionConfig.TryGetProperty("tradeInPrice", out var tradeInPriceElem) ||
                    !actionConfig.TryGetProperty("quantity", out var maxQuantityElem))
                {
                    continue;
                }
                tradeInPrice = tradeInPriceElem.GetDecimal();
                maxQuantity = maxQuantityElem.GetInt32();
            }
            catch (JsonException)
            {
                continue;
            }

            if (tradeInPrice <= 0 || maxQuantity < 1 || minAmount <= 0)
            {
                continue;
            }
            if (request.Amount < minAmount)
            {
                continue;
            }

            if (promotion.MaxUsagePerUser > 0 && request.MemberId.HasValue)
            {
                var used = await _dbContext.MallPromotionUsages
                    .AsNoTracking()
                    .CountAsync(u => u.PromotionId == promotion.Id
                        && u.UserId == request.MemberId.Value
                        && !u.IsDeleted, cancellationToken);
                if (used >= promotion.MaxUsagePerUser)
                {
                    continue;
                }
            }

            var skuRow = await (
                    from sku in _dbContext.MallSkus
                    join product in _dbContext.MallProducts on sku.ProductId equals product.Id
                    where sku.Id == skuId && !sku.IsDeleted && sku.IsActive && !product.IsDeleted && product.IsActive
                    select new { sku.SpecName, sku.Price, ProductName = product.Name, ProductImage = product.MainImageUrl })
                .FirstOrDefaultAsync(cancellationToken);

            if (skuRow == null)
            {
                continue;
            }

            result.Add(new MallExchangeOfferDto
            {
                PromotionId = promotion.Id,
                PromotionName = promotion.Name,
                MinAmount = minAmount,
                SkuId = skuId,
                ProductName = skuRow.ProductName,
                SpecName = skuRow.SpecName,
                ProductImage = skuRow.ProductImage,
                OriginalPrice = skuRow.Price,
                TradeInPrice = tradeInPrice,
                MaxQuantity = maxQuantity
            });
        }

        return result;
    }
}
