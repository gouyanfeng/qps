using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.EventDispatch;
using QPS.Application.Features.Crm;
using QPS.Application.Interfaces;
using QPS.Domain.Events.Crm;

namespace QPS.Application.Features.Crm.CrmHerbBaseSubjects;

/// <summary>
/// 药材基地主体评分重算事件处理器
/// </summary>
public sealed class RecalculateCrmHerbBaseSubjectScoreEventHandler
    : INotificationHandler<DomainEventNotification<CrmHerbBaseSubjectScoreAffectedEvent>>
{
    private readonly IDbContext _dbContext;

    public RecalculateCrmHerbBaseSubjectScoreEventHandler(IDbContext dbContext)
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
