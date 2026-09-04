using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Contracts.Crm.CrmFollowRecords;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;
using QPS.Domain.Events.Crm;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmFollowRecords;

public class CreateCrmFollowRecordCommand : IRequest<bool>
{
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public CrmFollowRecordCreateRequest Request { get; set; } = null!;
}

public class CreateCrmFollowRecordHandler : IRequestHandler<CreateCrmFollowRecordCommand, bool>
{
    private const string HerbBaseSubjectEntityType = CrmCodes.HerbBaseSubjectEntityType;
    private const string VendorEntityType = CrmCodes.VendorEntityType;

    private readonly IDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>
    /// 初始化通用跟进记录处理器。
    /// </summary>
    public CreateCrmFollowRecordHandler(IDbContext dbContext, ICurrentUserService currentUserService, IDomainEventDispatcher dispatcher)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// 为基地主体或厂商新增沟通记录，并同步其最近跟进摘要。
    /// </summary>
    public async Task<bool> Handle(CreateCrmFollowRecordCommand request, CancellationToken cancellationToken)
    {
        EnsureFollowResult(request.Request.FollowResult);
        EnsureSupportedEntityType(request.EntityType);
        var followAt = DateTime.Now;

        if (request.EntityType == HerbBaseSubjectEntityType)
        {
            var subject = await GetSubject(request.EntityId, cancellationToken);
            await EnsureContactBelongsToTarget(request, cancellationToken);
            _dbContext.CrmFollowRecords.Add(CreateFollowRecord(request));
            subject.UpdateFollowSummary(followAt, request.Request.FollowResult, request.Request.NextFollowAt);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _dispatcher.PublishAsync(new CrmHerbBaseSubjectScoreAffectedEvent(subject.Id), cancellationToken);
            return true;
        }

        var vendor = await GetVendor(request.EntityId, cancellationToken);
        await EnsureContactBelongsToTarget(request, cancellationToken);
        _dbContext.CrmFollowRecords.Add(CreateFollowRecord(request));
        vendor.UpdateFollowSummary(followAt, request.Request.FollowResult, request.Request.NextFollowAt);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void EnsureSupportedEntityType(string entityType)
    {
        if (entityType != HerbBaseSubjectEntityType && entityType != VendorEntityType)
        {
            throw new BusinessException(400, "不支持的沟通记录对象类型");
        }
    }

    private static void EnsureFollowResult(string followResult)
    {
        if (string.IsNullOrWhiteSpace(followResult))
        {
            throw new BusinessException(400, "沟通结果不能为空");
        }
    }

    private async Task<CrmHerbBaseSubject> GetSubject(Guid herbBaseSubjectId, CancellationToken cancellationToken)
    {
        var subject = await _dbContext.CrmHerbBaseSubjects.FirstOrDefaultAsync(subject => subject.Id == herbBaseSubjectId, cancellationToken);
        if (subject == null)
        {
            throw new BusinessException(404, "药材基地主体不存在");
        }

        return subject;
    }

    private async Task<CrmVendor> GetVendor(Guid vendorId, CancellationToken cancellationToken)
    {
        var vendor = await _dbContext.CrmVendors.FirstOrDefaultAsync(vendor => vendor.Id == vendorId, cancellationToken);
        if (vendor == null)
        {
            throw new BusinessException(404, "厂商不存在");
        }

        return vendor;
    }

    private async Task EnsureContactBelongsToTarget(CreateCrmFollowRecordCommand command, CancellationToken cancellationToken)
    {
        if (!command.Request.ContactId.HasValue)
        {
            return;
        }

        var contact = await _dbContext.CrmContacts.FirstOrDefaultAsync(c => c.Id == command.Request.ContactId.Value, cancellationToken);
        if (contact == null ||
            contact.EntityType != command.EntityType ||
            contact.EntityId != command.EntityId)
        {
            throw new BusinessException(404, "联系人不存在");
        }
    }

    private CrmFollowRecord CreateFollowRecord(CreateCrmFollowRecordCommand command)
    {
        var operatorUserId = GetOperatorUserId();

        return CrmFollowRecord.Create(
            command.EntityType,
            command.EntityId,
            command.Request.ContactId,
            command.Request.FollowType,
            command.Request.FollowResult,
            command.Request.IntentLevel,
            command.Request.Content,
            command.Request.NextFollowAt,
            operatorUserId);
    }

    private Guid? GetOperatorUserId()
    {
        return Guid.TryParse(_currentUserService.UserId, out var parsedUserId)
            ? parsedUserId
            : null;
    }
}
