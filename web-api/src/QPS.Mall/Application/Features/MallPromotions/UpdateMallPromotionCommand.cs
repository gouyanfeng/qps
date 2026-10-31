using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallPromotions;

public class UpdateMallPromotionCommand : IRequest<bool>
{
    public Guid PromotionId { get; set; }

    public MallPromotionUpdateRequest Request { get; set; } = null!;
}

public class UpdateMallPromotionHandler : IRequestHandler<UpdateMallPromotionCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public UpdateMallPromotionHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(UpdateMallPromotionCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        MallPromotionRules.ValidateUpdate(req);
        await MallPromotionRules.ValidateScopeTargetsAsync(
            _dbContext,
            req.ScopeType,
            req.Scopes,
            cancellationToken);

        var promotion = await _dbContext.MallPromotions
            .FirstOrDefaultAsync(p => p.Id == command.PromotionId && !p.IsDeleted, cancellationToken);

        if (promotion is null)
        {
            throw new BusinessException(404, "促销活动不存在");
        }

        promotion.Update(
            req.Name.Trim(),
            req.Description?.Trim() ?? string.Empty,
            req.TriggerType,
            req.TriggerConfigJson,
            req.ActionType,
            req.ActionConfigJson,
            req.ScopeType,
            req.MaxUsagePerUser,
            req.MaxUsagePerOrder,
            req.StartDate,
            req.EndDate);

        promotion.ChangeStatus(req.IsActive);

        var existingScopes = await _dbContext.MallPromotionScopes
            .Where(s => s.PromotionId == promotion.Id)
            .ToListAsync(cancellationToken);

        foreach (var scope in existingScopes)
        {
            scope.IsDeleted = true;
        }

        foreach (var scopeReq in req.Scopes.Where(_ => req.ScopeType != "Global"))
        {
            _dbContext.MallPromotionScopes.Add(MallPromotionScope.Create(
                promotion.Id, scopeReq.ScopeTargetType, scopeReq.ScopeTargetId));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}