using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Application.Features.MallCampaigns;

/// <summary>
/// 管理端分页查询活动专题。
/// </summary>
public class GetMallCampaignsQuery : PaginationRequest, IRequest<PaginationResponse<MallCampaignListItemDto>>
{
    public string? Keyword { get; set; }

    public bool? IsActive { get; set; }

    public int? Type { get; set; }
}

public class GetMallCampaignsHandler
    : IRequestHandler<GetMallCampaignsQuery, PaginationResponse<MallCampaignListItemDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallCampaignsHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallCampaignListItemDto>> Handle(
        GetMallCampaignsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var query = _dbContext.MallCampaigns.AsNoTracking().AsQueryable();

        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(campaign => campaign.Name.Contains(keyword));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(campaign => campaign.IsActive == request.IsActive.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(campaign => campaign.Type == request.Type.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var campaigns = await query
            .OrderBy(campaign => campaign.SortOrder)
            .ThenByDescending(campaign => campaign.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var campaignIds = campaigns.Select(campaign => campaign.Id).ToList();
        var productCountMap = campaignIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _dbContext.MallCampaignProducts
                .AsNoTracking()
                .Where(item => campaignIds.Contains(item.CampaignId))
                .GroupBy(item => item.CampaignId)
                .Select(group => new { CampaignId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.CampaignId, item => item.Count, cancellationToken);

        var now = DateTime.Now;
        var list = campaigns
            .Select(campaign => MallCampaignMapper.ToListItem(
                campaign,
                productCountMap.GetValueOrDefault(campaign.Id),
                now))
            .ToList();

        return new PaginationResponse<MallCampaignListItemDto>(list, totalCount, page, pageSize);
    }
}
