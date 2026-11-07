using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts.DTOs;
using QPS.Crm.Contracts.DTOs.CrmContacts;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;
using QPS.Crm.Domain.Events;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmContacts;

public class CreateCrmContactCommand : IRequest<bool>
{
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public CrmContactCreateRequest Request { get; set; } = null!;
}

public class CreateCrmContactHandler : IRequestHandler<CreateCrmContactCommand, bool>
{
    private const string InvalidStatus = "无效";

    private readonly ICrmDbContext _dbContext;
    private readonly IDomainEventDispatcher _dispatcher;

    public CreateCrmContactHandler(ICrmDbContext dbContext, IDomainEventDispatcher dispatcher)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
    }

    public async Task<bool> Handle(CreateCrmContactCommand request, CancellationToken cancellationToken)
    {
        var target = new CrmContactTarget(request.EntityType, request.EntityId);
        target.EnsureSupported();

        var subject = target.IsHerbBaseSubject
            ? await GetSubject(target.EntityId, cancellationToken)
            : null;
        if (target.IsVendor)
        {
            await EnsureVendorExists(target.EntityId, cancellationToken);
        }

        await EnsurePhoneNotDuplicated(target, request.Request.Phone, null, cancellationToken);

        var contact = CrmContact.Create(
            target.EntityType,
            target.EntityId,
            request.Request.ContactName,
            request.Request.Phone,
            request.Request.PhoneType,
            request.Request.Wechat,
            request.Request.RoleName,
            await ShouldBePrimary(target, subject, request.Request.IsPrimary, cancellationToken),
            request.Request.Remark);

        if (contact.IsPrimary)
        {
            await UnmarkSiblingPrimaryContacts(target, contact.Id, cancellationToken);
            subject?.UpdatePrimaryContact(contact.ContactName, contact.Phone);
        }

        _dbContext.CrmContacts.Add(contact);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await PublishScoreIfNeeded(subject, cancellationToken);

        return true;
    }

    private async Task<CrmHerbBaseSubject> GetSubject(Guid subjectId, CancellationToken cancellationToken)
    {
        var subject = await _dbContext.CrmHerbBaseSubjects
            .FirstOrDefaultAsync(item => item.Id == subjectId, cancellationToken);

        return subject ?? throw new BusinessException(404, "药材基地主体不存在");
    }

    private async Task EnsureVendorExists(Guid vendorId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.CrmVendors
            .AnyAsync(item => item.Id == vendorId, cancellationToken);

        if (!exists)
        {
            throw new BusinessException(404, "厂商不存在");
        }
    }

    private async Task EnsurePhoneNotDuplicated(
        CrmContactTarget target,
        string phone,
        Guid? excludedContactId,
        CancellationToken cancellationToken)
    {
        var normalizedPhone = phone?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return;
        }

        var exists = await _dbContext.CrmContacts.AnyAsync(
            contact =>
                contact.EntityType == target.EntityType &&
                contact.EntityId == target.EntityId &&
                contact.Id != excludedContactId &&
                contact.Phone == normalizedPhone,
            cancellationToken);

        if (exists)
        {
            throw new BusinessException(400, "该对象下已存在相同联系电话");
        }
    }

    private async Task<bool> ShouldBePrimary(
        CrmContactTarget target,
        CrmHerbBaseSubject? subject,
        bool requestedPrimary,
        CancellationToken cancellationToken)
    {
        if (requestedPrimary)
        {
            return true;
        }

        if (target.IsHerbBaseSubject)
        {
            return string.IsNullOrWhiteSpace(subject?.PrimaryContactName) &&
                string.IsNullOrWhiteSpace(subject?.PrimaryContactPhone);
        }

        return !await _dbContext.CrmContacts.AnyAsync(
            contact =>
                contact.EntityType == target.EntityType &&
                contact.EntityId == target.EntityId &&
                contact.Status != InvalidStatus,
            cancellationToken);
    }

    private async Task UnmarkSiblingPrimaryContacts(
        CrmContactTarget target,
        Guid contactId,
        CancellationToken cancellationToken)
    {
        var siblings = await _dbContext.CrmContacts
            .Where(contact =>
                contact.EntityType == target.EntityType &&
                contact.EntityId == target.EntityId &&
                contact.Id != contactId &&
                contact.IsPrimary)
            .ToListAsync(cancellationToken);

        foreach (var sibling in siblings)
        {
            sibling.UnmarkPrimary();
        }
    }

    private async Task PublishScoreIfNeeded(CrmHerbBaseSubject? subject, CancellationToken cancellationToken)
    {
        if (subject != null)
        {
            await _dispatcher.PublishAsync(new CrmHerbBaseSubjectScoreAffectedEvent(subject.Id), cancellationToken);
        }
    }
}
