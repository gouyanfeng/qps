using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Application.Features.MallCampaigns;

/// <summary>
/// 移动端活动专题列表：仅返回进行中（已上架、未删除、位于时间窗内）的专题。
/// </summary>
public class GetPublishedMallCampaignsQuery : IRequest<List<MallCampaignPublicListItemDto>>
{
}

public class GetPublishedMallCampaignsHandler
    : IRequestHandler<GetPublishedMallCampaignsQuery, List<MallCampaignPublicListItemDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetPublishedMallCampaignsHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MallCampaignPublicListItemDto>> Handle(
        GetPublishedMallCampaignsQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;

        var campaigns = await _dbContext.MallCampaigns
            .AsNoTracking()
            .Where(campaign =>
                campaign.IsActive &&
                campaign.StartAt <= now &&
                campaign.EndAt > now)
            .OrderBy(campaign => campaign.SortOrder)
            .ThenByDescending(campaign => campaign.CreatedAt)
            .ToListAsync(cancellationToken);

        return campaigns
            .Select(campaign => MallCampaignMapper.ToPublicListItem(campaign, now))
            .ToList();
    }
}
