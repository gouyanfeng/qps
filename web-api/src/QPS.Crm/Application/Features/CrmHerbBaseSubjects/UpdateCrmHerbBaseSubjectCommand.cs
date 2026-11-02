using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts.DTOs;
using QPS.Crm.Contracts.DTOs.CrmHerbBaseSubjects;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Events;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmHerbBaseSubjects;

public class UpdateCrmHerbBaseSubjectCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public CrmHerbBaseSubjectUpdateRequest Request { get; set; } = null!;
}

public class UpdateCrmHerbBaseSubjectHandler : IRequestHandler<UpdateCrmHerbBaseSubjectCommand, bool>
{
    private readonly ICrmDbContext _dbContext;
    private readonly IDomainEventDispatcher _dispatcher;

    public UpdateCrmHerbBaseSubjectHandler(ICrmDbContext dbContext, IDomainEventDispatcher dispatcher)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
    }

    public async Task<bool> Handle(UpdateCrmHerbBaseSubjectCommand request, CancellationToken cancellationToken)
    {
        var subject = await _dbContext.CrmHerbBaseSubjects
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
        if (subject == null)
            throw new BusinessException(404, "药材基地主体不存在");

        subject.UpdateBasicInfo(
            request.Request.SubjectName,
            request.Request.SubjectType,
            request.Request.Status,
            request.Request.Grade,
            request.Request.Score,
            request.Request.Remark);

        var syncedSubjectName = subject.SubjectName ?? string.Empty;
        var herbBases = await _dbContext.CrmHerbBases
            .Where(item => item.HerbBaseSubjectId == subject.Id)
            .ToListAsync(cancellationToken);

        foreach (var herbBase in herbBases)
        {
            herbBase.RenameSubject(syncedSubjectName);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _dispatcher.PublishAsync(new CrmHerbBaseSubjectScoreAffectedEvent(subject.Id), cancellationToken);
        return true;
    }
}
