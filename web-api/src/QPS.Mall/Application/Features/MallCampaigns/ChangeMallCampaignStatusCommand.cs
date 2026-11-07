using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCampaigns;

/// <summary>
/// 管理端活动专题上下架。
/// </summary>
public class ChangeMallCampaignStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public bool IsActive { get; set; }
}

public class ChangeMallCampaignStatusHandler : IRequestHandler<ChangeMallCampaignStatusCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public ChangeMallCampaignStatusHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(
        ChangeMallCampaignStatusCommand command,
        CancellationToken cancellationToken)
    {
        var campaign = await _dbContext.MallCampaigns
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);

        if (campaign is null)
        {
            throw new BusinessException(404, "活动专题不存在");
        }

        campaign.ChangeStatus(command.IsActive);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
