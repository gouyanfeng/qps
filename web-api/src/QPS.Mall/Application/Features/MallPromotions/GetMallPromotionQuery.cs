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
            .Include(x => x.Scopes)
            .FirstOrDefaultAsync(x => x.Id == request.PromotionId && !x.IsDeleted, cancellationToken);

        if (p is null)
        {
            throw new BusinessException(404, "促销活动不存在");
        }

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
            Priority = p.Priority,
            Combinable = p.Combinable,
            MaxUsagePerUser = p.MaxUsagePerUser,
            MaxUsagePerOrder = p.MaxUsagePerOrder,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Scopes = p.Scopes.Select(s => new MallPromotionScopeDto
            {
                Id = s.Id,
                ScopeTargetType = s.ScopeTargetType,
                ScopeTargetId = s.ScopeTargetId,
                ScopeTargetName = $"{s.ScopeTargetType}:{s.ScopeTargetId}"
            }).ToList()
        };
    }
}