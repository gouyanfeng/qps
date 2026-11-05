using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;

namespace QPS.Mall.Application.Features.MallPromotions;

public class GetMallPromotionsQuery : IRequest<PaginationResponse<MallPromotionDto>>
{
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
    public string? ActionType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class GetMallPromotionsHandler : IRequestHandler<GetMallPromotionsQuery, PaginationResponse<MallPromotionDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallPromotionsHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallPromotionDto>> Handle(
        GetMallPromotionsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.MallPromotions
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var kw = request.Keyword.Trim();
            query = query.Where(p => p.Name.Contains(kw) || p.Description.Contains(kw));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ActionType))
        {
            var at = request.ActionType.Trim();
            query = query.Where(p => p.ActionType == at);
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : request.PageSize;

        var totalCount = await query.CountAsync(cancellationToken);
        var promotions = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var promotionIds = promotions.Select(p => p.Id).ToList();
        var allScopes = promotionIds.Count == 0
            ? new List<Domain.Entities.MallPromotionScope>()
            : await _dbContext.MallPromotionScopes
                .Where(s => promotionIds.Contains(s.PromotionId))
                .ToListAsync(cancellationToken);

        var scopeMap = allScopes.GroupBy(s => s.PromotionId).ToDictionary(g => g.Key, g => g.ToList());

        var list = promotions.Select(p => Map(p, scopeMap.TryGetValue(p.Id, out var scopes) ? scopes : new List<Domain.Entities.MallPromotionScope>())).ToList();

        return new PaginationResponse<MallPromotionDto>(list, totalCount, page, pageSize);
    }

    private static MallPromotionDto Map(Domain.Entities.MallPromotion p, List<Domain.Entities.MallPromotionScope> scopes) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        PromotionType = p.PromotionType,
        ThresholdAmount = p.ThresholdAmount,
        ActionType = p.ActionType,
        ActionConfigJson = p.ActionConfigJson,
        PromotionTagId = p.PromotionTagId,
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