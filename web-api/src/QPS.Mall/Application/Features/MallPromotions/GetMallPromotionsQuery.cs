using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;

namespace QPS.Mall.Application.Features.MallPromotions;

public class GetMallPromotionsQuery : IRequest<List<MallPromotionDto>>
{
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
}

public class GetMallPromotionsHandler : IRequestHandler<GetMallPromotionsQuery, List<MallPromotionDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallPromotionsHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MallPromotionDto>> Handle(
        GetMallPromotionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MallPromotions
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            query = query.Where(p => p.Name.Contains(kw) || p.Description.Contains(kw));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == request.IsActive.Value);
        }

        var promotions = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var promotionIds = promotions.Select(p => p.Id).ToList();
        var allScopes = promotionIds.Count == 0
            ? new List<Domain.Entities.MallPromotionScope>()
            : await _dbContext.MallPromotionScopes
                .Where(s => promotionIds.Contains(s.PromotionId) && !s.IsDeleted)
                .ToListAsync(cancellationToken);

        var scopeMap = allScopes.GroupBy(s => s.PromotionId).ToDictionary(g => g.Key, g => g.ToList());

        return promotions.Select(p => Map(p, scopeMap.TryGetValue(p.Id, out var scopes) ? scopes : new List<Domain.Entities.MallPromotionScope>())).ToList();
    }

    private static MallPromotionDto Map(Domain.Entities.MallPromotion p, List<Domain.Entities.MallPromotionScope> scopes) => new()
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