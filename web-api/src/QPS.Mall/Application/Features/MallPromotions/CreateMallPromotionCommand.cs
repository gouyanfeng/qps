using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallPromotions;

public class CreateMallPromotionCommand : IRequest<Guid>
{
    public MallPromotionCreateRequest Request { get; set; } = null!;
}

public class CreateMallPromotionHandler : IRequestHandler<CreateMallPromotionCommand, Guid>
{
    private readonly IMallDbContext _dbContext;

    public CreateMallPromotionHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateMallPromotionCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        MallPromotionRules.ValidateCreate(req);
        await MallPromotionRules.ValidateScopeTargetsAsync(
            _dbContext,
            req.ScopeType,
            req.Scopes,
            cancellationToken);

        var promotion = MallPromotion.Create(
            req.Name.Trim(),
            req.Description?.Trim() ?? string.Empty,
            req.TriggerType,
            req.TriggerConfigJson,
            req.ActionType,
            req.ActionConfigJson,
            req.ScopeType,
            req.Priority,
            req.Combinable,
            req.MaxUsagePerUser,
            req.MaxUsagePerOrder,
            req.StartDate,
            req.EndDate,
            req.IsActive);

        _dbContext.MallPromotions.Add(promotion);

        foreach (var scopeReq in req.Scopes.Where(_ => req.ScopeType != "Global"))
        {
            promotion.AddScope(MallPromotionScope.Create(
                promotion.Id, scopeReq.ScopeTargetType, scopeReq.ScopeTargetId));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return promotion.Id;
    }
}