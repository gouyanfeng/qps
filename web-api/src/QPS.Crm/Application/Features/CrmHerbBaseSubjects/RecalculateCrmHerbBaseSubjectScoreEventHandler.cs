using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Shared.Pipeline;
using QPS.Crm.Application.Features;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Events;

namespace QPS.Crm.Application.Features.CrmHerbBaseSubjects;

/// <summary>
/// 药材基地主体评分重算事件处理器
/// </summary>
public sealed class RecalculateCrmHerbBaseSubjectScoreEventHandler
    : INotificationHandler<DomainEventNotification<CrmHerbBaseSubjectScoreAffectedEvent>>
{
    private readonly ICrmDbContext _dbContext;

    public RecalculateCrmHerbBaseSubjectScoreEventHandler(ICrmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DomainEventNotification<CrmHerbBaseSubjectScoreAffectedEvent> notification, CancellationToken cancellationToken)
    {
        var subjectId = notification.DomainEvent.SubjectId;
        var scoreInput = await CrmHerbBaseSubjectScoreInputBuilder.BuildAsync(
            _dbContext,
            subjectId,
            cancellationToken);

        if (scoreInput == null)
        {
            return;
        }

        var subject = await _dbContext.CrmHerbBaseSubjects
            .FirstOrDefaultAsync(item => item.Id == subjectId, cancellationToken);

        if (subject == null)
        {
            return;
        }

        subject.RecalculateScoreGrade(scoreInput);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
