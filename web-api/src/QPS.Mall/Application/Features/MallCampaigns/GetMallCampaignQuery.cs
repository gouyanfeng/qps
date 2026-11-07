using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCampaigns;

/// <summary>
/// 管理端查询活动专题详情（含已选商品）。
/// </summary>
public class GetMallCampaignQuery : IRequest<MallCampaignDto>
{
    public Guid Id { get; set; }
}

public class GetMallCampaignHandler : IRequestHandler<GetMallCampaignQuery, MallCampaignDto>
{
    private readonly IMallDbContext _dbContext;

    public GetMallCampaignHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallCampaignDto> Handle(
        GetMallCampaignQuery request,
        CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.MallCampaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (campaign is null)
        {
            throw new BusinessException(404, "活动专题不存在");
        }

        var items = await LoadItemsAsync(campaign.Id, cancellationToken);
        var coupons = await MallCampaignCouponLoader.LoadAsync(_dbContext, campaign.Id, cancellationToken);

        return MallCampaignMapper.ToDetail(campaign, items, coupons, DateTime.Now);
    }

    /// <summary>
    /// 装载选品行：商品信息单独查询后手动组装，不使用导航属性。
    /// </summary>
    private async Task<List<MallCampaignProductItemDto>> LoadItemsAsync(
        Guid campaignId,
        CancellationToken cancellationToken)
    {
        var links = await _dbContext.MallCampaignProducts
            .AsNoTracking()
            .Where(item => item.CampaignId == campaignId)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        if (links.Count == 0)
        {
            return new List<MallCampaignProductItemDto>();
        }

        var productIds = links.Select(link => link.ProductId).Distinct().ToList();

        var products = await _dbContext.MallProducts
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new
            {
                product.Id,
                product.Name,
                product.Code,
                product.MainImageUrl,
                product.IsActive,
                product.IsDeleted
            })
            .ToListAsync(cancellationToken);

        var minPrices = await _dbContext.MallSkus
            .AsNoTracking()
            .Where(sku => productIds.Contains(sku.ProductId) && sku.IsActive)
            .GroupBy(sku => sku.ProductId)
            .Select(group => new { ProductId = group.Key, MinPrice = group.Min(sku => sku.Price) })
            .ToDictionaryAsync(item => item.ProductId, item => item.MinPrice, cancellationToken);

        var productMap = products.ToDictionary(product => product.Id);

        return links.Select(link =>
        {
            var product = productMap.GetValueOrDefault(link.ProductId);

            return new MallCampaignProductItemDto
            {
                Id = link.Id,
                ProductId = link.ProductId,
                ProductName = product?.Name ?? "（商品已删除）",
                ProductCode = product?.Code ?? string.Empty,
                MainImageUrl = product?.MainImageUrl ?? string.Empty,
                MinPrice = minPrices.TryGetValue(link.ProductId, out var minPrice) ? minPrice : null,
                ProductIsActive = product is not null && product.IsActive && !product.IsDeleted,
                GroupName = link.GroupName,
                SortOrder = link.SortOrder
            };
        }).ToList();
    }
}
