using MediatR;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;

namespace QPS.Mall.Application.Features.MallActivities;

public class CreateMallActivityCommand : IRequest<Guid>
{
    public MallActivityCreateRequest Request { get; set; } = null!;
}

public class CreateMallActivityHandler : IRequestHandler<CreateMallActivityCommand, Guid>
{
    private readonly IMallDbContext _dbContext;

    public CreateMallActivityHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateMallActivityCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;

        var name = MallActivityRules.NormalizeName(req.Name);
        var subtitle = MallActivityRules.NormalizeSubtitle(req.Subtitle);
        var coverImage = MallActivityRules.NormalizeCoverImage(req.CoverImage);
        var description = MallActivityRules.NormalizeDescription(req.Description);
        var detailHtml = MallActivityRules.NormalizeDetailHtml(req.DetailHtml);
        var sortOrder = MallActivityRules.NormalizeSortOrder(req.SortOrder);
        MallActivityRules.ValidateTimeWindow(req.StartDate, req.EndDate);

        var activity = MallActivity.Create(
            name,
            subtitle,
            coverImage,
            description,
            detailHtml,
            req.StartDate,
            req.EndDate,
            sortOrder,
            req.IsActive);

        _dbContext.MallActivities.Add(activity);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return activity.Id;
    }
}