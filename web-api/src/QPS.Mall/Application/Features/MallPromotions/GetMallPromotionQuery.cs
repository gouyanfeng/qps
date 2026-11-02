using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallPromotions;

public class GetMallPromotionQuery : IRequest<MallPromotionDto>
{
    public Guid PromotionId { get; set; }
}

public class GetMallPromotionHandler : IRequestHandler<GetMallPromotionQuery, MallPromotionDto>
{
    private readonly IMallDbContext _dbContext;

    public GetMallPromotionHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallPromotionDto> Handle(
        GetMallPromotionQuery request,
        CancellationToken cancellationToken)
    {
        var p = await _dbContext.MallPromotions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.PromotionId && !x.IsDeleted, cancellationToken);

        if (p is null)
        {
            throw new BusinessException(404, "促销活动不存在");
        }

        var scopes = await _dbContext.MallPromotionScopes
            .Where(s => s.PromotionId == p.Id && !s.IsDeleted)
            .ToListAsync(cancellationToken);

        return new MallPromotionDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            TriggerType = p.TriggerType,
            TriggerConfigJson = p.TriggerConfigJson,
            ActionType = p.ActionType,
            ActionConfigJson = p.ActionConfigJson,
            ScopeType = p.ScopeType,
            MaxUsagePerUser = p.MaxUsagePerUser,
            MaxUsagePerOrder = p.MaxUsagePerOrder,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Scopes = scopes.Select(s => new MallPromotionScopeDto
            {
                Id = s.Id,
                ScopeTargetType = s.ScopeTargetType,
                ScopeTargetId = s.ScopeTargetId,
                ScopeTargetName = $"{s.ScopeTargetType}:{s.ScopeTargetId}"
            }).ToList()
        };
    }
}