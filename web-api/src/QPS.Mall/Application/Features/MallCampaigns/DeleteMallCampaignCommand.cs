using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCampaigns;

/// <summary>
/// 管理端删除活动专题（软删除，同时软删除全部选品）。
/// </summary>
public class DeleteMallCampaignCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class DeleteMallCampaignHandler : IRequestHandler<DeleteMallCampaignCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public DeleteMallCampaignHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(
        DeleteMallCampaignCommand command,
        CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.MallCampaigns
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);

        if (campaign is null)
        {
            throw new BusinessException(404, "活动专题不存在");
        }

        campaign.SoftDelete();

        var items = await _dbContext.MallCampaignProducts
            .Where(item => item.CampaignId == campaign.Id)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.SoftDelete();
        }

        var coupons = await _dbContext.MallCampaignCoupons
            .Where(item => item.CampaignId == campaign.Id)
            .ToListAsync(cancellationToken);

        foreach (var coupon in coupons)
        {
            coupon.SoftDelete();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
