using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallActivities;

public class GetMallActivityQuery : IRequest<MallActivityDto>
{
    public Guid Id { get; set; }
}

public class GetMallActivityHandler : IRequestHandler<GetMallActivityQuery, MallActivityDto>
{
    private readonly IMallDbContext _dbContext;

    public GetMallActivityHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallActivityDto> Handle(GetMallActivityQuery query, CancellationToken cancellationToken)
    {
        var activity = await _dbContext.MallActivities
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == query.Id, cancellationToken);

        if (activity is null)
        {
            throw new BusinessException(404, "活动不存在");
        }

        var links = await _dbContext.MallActivityPromotions
            .AsNoTracking()
            .Where(link => link.ActivityId == activity.Id && !link.IsDeleted)
            .ToListAsync(cancellationToken);

        var promotionIds = links.Select(link => link.PromotionId).Distinct().ToList();
        var promotions = promotionIds.Count == 0
            ? new Dictionary<Guid, QPS.Mall.Domain.Entities.MallPromotion>()
            : await _dbContext.MallPromotions
                .AsNoTracking()
                .Where(promotion => promotionIds.Contains(promotion.Id))
                .ToDictionaryAsync(promotion => promotion.Id, cancellationToken);

        return MallActivityMapper.ToDetail(activity, links, promotions);
    }
}