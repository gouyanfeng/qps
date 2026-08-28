namespace QPS.Domain.Events.Crm;

/// <summary>
/// 药材基地主体评分受影响领域事件
/// </summary>
public sealed class CrmHerbBaseSubjectScoreAffectedEvent : IDomainEvent
{
    public CrmHerbBaseSubjectScoreAffectedEvent(Guid subjectId)
    {
        SubjectId = subjectId;
    }

    public Guid SubjectId { get; }
}
