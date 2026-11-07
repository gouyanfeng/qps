using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCampaigns;

/// <summary>
/// 移动端活动专题详情：未开始/已结束的专题仍可查看，便于前端展示状态差异。
/// </summary>
public class GetPublishedMallCampaignDetailQuery : IRequest<MallCampaignPublicDetailDto>
{
    public Guid Id { get; set; }
}

public class GetPublishedMallCampaignDetailHandler
    : IRequestHandler<GetPublishedMallCampaignDetailQuery, MallCampaignPublicDetailDto>
{
    private readonly IMallDbContext _dbContext;

    public GetPublishedMallCampaignDetailHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallCampaignPublicDetailDto> Handle(
        GetPublishedMallCampaignDetailQuery request,
        CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.MallCampaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == request.Id && item.IsActive,
                cancellationToken);

        if (campaign is null)
        {
            throw new BusinessException(404, "活动专题不存在或已下架");
        }

        var coupons = await MallCampaignCouponLoader.LoadAsync(_dbContext, campaign.Id, cancellationToken);

        return MallCampaignMapper.ToPublicDetail(campaign, coupons, DateTime.Now);
    }
}
