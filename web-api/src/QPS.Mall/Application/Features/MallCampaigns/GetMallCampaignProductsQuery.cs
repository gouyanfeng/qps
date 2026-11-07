using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallProducts;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCampaigns;

/// <summary>
/// 移动端活动专题商品分页：商品 DTO 与促销富化结果与商品列表接口保持一致。
/// </summary>
public class GetMallCampaignProductsQuery : PaginationRequest, IRequest<PaginationResponse<MallCampaignProductDto>>
{
    public Guid CampaignId { get; set; }
}

public class GetMallCampaignProductsHandler
    : IRequestHandler<GetMallCampaignProductsQuery, PaginationResponse<MallCampaignProductDto>>
{
    private readonly IMallDbContext _dbContext;
    private readonly MallProductPromotionEnricher _enricher;

    public GetMallCampaignProductsHandler(IMallDbContext dbContext, MallProductPromotionEnricher enricher)
    {
        _dbContext = dbContext;
        _enricher = enricher;
    }

    public async Task<PaginationResponse<MallCampaignProductDto>> Handle(
        GetMallCampaignProductsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var campaign = await _dbContext.MallCampaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.CampaignId, cancellationToken);

        if (campaign is null || !campaign.IsActive)
        {
            throw new BusinessException(404, "活动专题不存在或已下架");
        }

        var now = DateTime.Now;
        if (campaign.HasEnded(now))
        {
            throw new BusinessException(400, "活动已结束");
        }

        var query = from link in _dbContext.MallCampaignProducts
                    join product in _dbContext.MallProducts on link.ProductId equals product.Id
                    where link.CampaignId == campaign.Id && product.IsActive
                    select new { link, product };

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .OrderBy(item => item.link.SortOrder)
            .ThenBy(item => item.link.CreatedAt)
            .ThenBy(item => item.link.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                item.link.Id,
                item.link.GroupName,
                item.link.SortOrder,
                Product = item.product,
                StoreName = _dbContext.MallStores
                    .Where(store => store.Id == item.product.StoreId)
                    .Select(store => store.Name)
                    .FirstOrDefault(),
                CategoryName = _dbContext.MallCategories
                    .Where(category => category.Id == item.product.CategoryId)
                    .Select(category => category.Name)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var productDtos = rows
            .Select(row => MallProductMapper.Map(
                row.Product,
                row.StoreName ?? string.Empty,
                row.CategoryName ?? string.Empty))
            .ToList();

        await MallProductBatchLoader.LoadSkusAndTagsAsync(_dbContext, productDtos, cancellationToken);
        await _enricher.EnrichAsync(productDtos, cancellationToken);

        var list = rows.Select((row, index) => new MallCampaignProductDto
        {
            CampaignProductId = row.Id,
            GroupName = row.GroupName,
            SortOrder = row.SortOrder,
            Product = productDtos[index]
        }).ToList();

        return new PaginationResponse<MallCampaignProductDto>(list, totalCount, page, pageSize);
    }
}
