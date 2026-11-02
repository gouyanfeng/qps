using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallActivities;

public class UpdateMallActivityCommand : IRequest<Guid>
{
    public Guid Id { get; set; }

    public MallActivityUpdateRequest Request { get; set; } = null!;
}

public class UpdateMallActivityHandler : IRequestHandler<UpdateMallActivityCommand, Guid>
{
    private readonly IMallDbContext _dbContext;

    public UpdateMallActivityHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(UpdateMallActivityCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        var activity = await _dbContext.MallActivities
            .FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);

        if (activity is null)
        {
            throw new BusinessException(404, "活动不存在");
        }

        var name = MallActivityRules.NormalizeName(req.Name);
        var subtitle = MallActivityRules.NormalizeSubtitle(req.Subtitle);
        var coverImage = MallActivityRules.NormalizeCoverImage(req.CoverImage);
        var description = MallActivityRules.NormalizeDescription(req.Description);
        var detailHtml = MallActivityRules.NormalizeDetailHtml(req.DetailHtml);
        var sortOrder = MallActivityRules.NormalizeSortOrder(req.SortOrder);
        MallActivityRules.ValidateTimeWindow(req.StartDate, req.EndDate);
        await MallActivityRules.EnsurePromotionsExistAsync(_dbContext, req.PromotionIds, cancellationToken);

        activity.Update(
            name,
            subtitle,
            coverImage,
            description,
            detailHtml,
            req.StartDate,
            req.EndDate,
            sortOrder);

        var existingLinks = await _dbContext.MallActivityPromotions
            .Where(link => link.ActivityId == activity.Id && !link.IsDeleted)
            .ToListAsync(cancellationToken);
        foreach (var link in existingLinks)
        {
            link.IsDeleted = true;
        }

        foreach (var promotionId in req.PromotionIds.Distinct())
        {
            _dbContext.MallActivityPromotions.Add(MallActivityPromotion.Create(activity.Id, promotionId));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return activity.Id;
    }
}