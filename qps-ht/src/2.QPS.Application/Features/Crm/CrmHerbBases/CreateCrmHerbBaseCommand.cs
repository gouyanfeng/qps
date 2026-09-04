using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;
using QPS.Domain.Events.Crm;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmHerbBases;

public class CreateCrmHerbBaseCommand : IRequest<bool>
{
    public CrmHerbBaseCreateRequest Request { get; set; } = null!;
}

public class CreateCrmHerbBaseHandler : IRequestHandler<CreateCrmHerbBaseCommand, bool>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDomainEventDispatcher _dispatcher;

    public CreateCrmHerbBaseHandler(
        IDbContext dbContext,
        ICurrentUserService currentUserService,
        IDomainEventDispatcher dispatcher)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _dispatcher = dispatcher;
    }

    public async Task<bool> Handle(CreateCrmHerbBaseCommand request, CancellationToken cancellationToken)
    {
        var operatorUserId = GetOperatorUserId();
        await EnsureOperatorActiveAsync(operatorUserId, cancellationToken);
        var herbBase = CreateHerbBase(request.Request);
        var (subject, isNewSubject) = await ResolveSubjectAsync(request.Request, herbBase.BaseName, operatorUserId, cancellationToken);
        herbBase.SetHerbBaseSubject(subject.Id);
        await SyncSubjectScaleAsync(subject, herbBase.Scale ?? 0, isNewSubject, cancellationToken);

        if (isNewSubject)
        {
            ApplyPrimaryContact(subject, request.Request);
            _dbContext.CrmHerbBaseSubjects.Add(subject);
            _dbContext.CrmTransferRecords.Add(CrmTransferRecord.CreateEntry(
                CrmTransferEntityType.HerbBaseSubject,
                subject.Id,
                subject.OwnerUserId,
                operatorUserId,
                request.Request.Remark.Trim()));
        }

        _dbContext.CrmHerbBases.Add(herbBase);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (herbBase.HerbBaseSubjectId.HasValue)
        {
            await _dispatcher.PublishAsync(new CrmHerbBaseSubjectScoreAffectedEvent(herbBase.HerbBaseSubjectId.Value), cancellationToken);
        }

        return true;
    }

    private static CrmHerbBase CreateHerbBase(CrmHerbBaseCreateRequest request)
    {
        var baseName = request.BaseName;

        return CrmHerbBase.Create(
            herbBaseName: baseName,
            grade: request.Grade,
            score: request.Score,
            province: request.Province,
            city: request.City,
            area: request.Area,
            address: request.Address,
            lat: request.Lat,
            lng: request.Lng,
            sourcePlatform: request.SourcePlatform,
            sourceId: request.SourceId,
            ownerUserId: null,
            remark: request.Remark,
            subjectName: request.SubjectName,
            scale: request.Scale);
    }

    private static CrmHerbBaseSubject CreateSubject(CrmHerbBaseCreateRequest request, string baseName, Guid ownerUserId)
    {
        var hasSubjectName = !string.IsNullOrWhiteSpace(request.SubjectName);
        return CrmHerbBaseSubject.Create(
            request.SubjectName,
            baseName,
            hasSubjectName ? "未知" : "仅基地",
            ownerUserId,
            CrmCodes.Status.Pending,
            request.Grade,
            request.Score,
            request.Remark,
            request.Scale);
    }

    private async Task<(CrmHerbBaseSubject Subject, bool IsNew)> ResolveSubjectAsync(
        CrmHerbBaseCreateRequest request,
        string baseName,
        Guid operatorUserId,
        CancellationToken cancellationToken)
    {
        if (request.HerbBaseSubjectId.HasValue)
        {
            var subject = await _dbContext.CrmHerbBaseSubjects
                .FirstOrDefaultAsync(subject => subject.Id == request.HerbBaseSubjectId.Value, cancellationToken);
            return subject == null
                ? throw new BusinessException(404, "药材基地主体不存在")
                : (subject, false);
        }

        if (string.IsNullOrWhiteSpace(request.SubjectName))
        {
            return (CreateSubject(request, baseName, operatorUserId), true);
        }

        var subjectName = request.SubjectName.Trim();
        var existingSubject = await _dbContext.CrmHerbBaseSubjects
            .FirstOrDefaultAsync(subject => subject.SubjectName == subjectName, cancellationToken);

        return existingSubject == null
            ? (CreateSubject(request, baseName, operatorUserId), true)
            : (existingSubject, false);
    }

    private static void ApplyPrimaryContact(CrmHerbBaseSubject subject, CrmHerbBaseCreateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PrimaryContactName) &&
            string.IsNullOrWhiteSpace(request.PrimaryContactPhone))
        {
            return;
        }

        subject.UpdatePrimaryContact(
            request.PrimaryContactName ?? string.Empty,
            request.PrimaryContactPhone ?? string.Empty);
    }

    private async Task SyncSubjectScaleAsync(
        CrmHerbBaseSubject subject,
        decimal newBaseScale,
        bool isNewSubject,
        CancellationToken cancellationToken)
    {
        var existingScale = isNewSubject
            ? 0
            : await _dbContext.CrmHerbBases
                .Where(herbBase => herbBase.HerbBaseSubjectId == subject.Id)
                .SumAsync(herbBase => herbBase.Scale ?? 0, cancellationToken);

        subject.UpdateScale(existingScale + newBaseScale);
    }

    private Guid GetOperatorUserId()
    {
        return Guid.TryParse(_currentUserService.UserId, out var operatorUserId)
            ? operatorUserId
            : throw new BusinessException(401, "登录状态无效");
    }

    private async Task EnsureOperatorActiveAsync(Guid operatorUserId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.SystemUsers.AnyAsync(user => user.Id == operatorUserId && user.IsActive, cancellationToken))
            throw new BusinessException(401, "当前用户不可用");
    }
}
