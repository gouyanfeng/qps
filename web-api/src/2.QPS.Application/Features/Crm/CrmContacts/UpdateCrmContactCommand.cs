using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Contracts.Crm.CrmContacts;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;
using QPS.Domain.Events.Crm;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmContacts;

public class UpdateCrmContactCommand : IRequest<bool>
{
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public Guid Id { get; set; }

    public CrmContactUpdateRequest Request { get; set; } = null!;
}

public class UpdateCrmContactHandler : IRequestHandler<UpdateCrmContactCommand, bool>
{
    private const string InvalidStatus = "无效";

    private readonly IDbContext _dbContext;
    private readonly IDomainEventDispatcher _dispatcher;

    public UpdateCrmContactHandler(IDbContext dbContext, IDomainEventDispatcher dispatcher)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
    }

    public async Task<bool> Handle(UpdateCrmContactCommand request, CancellationToken cancellationToken)
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

        var contact = await GetContact(target, request.Id, cancellationToken);
        EnsureCanSetPrimary(request.Request, contact);
        await EnsurePhoneNotDuplicated(target, contact.Id, request.Request.Phone, cancellationToken);

        var wasPrimary = contact.IsPrimary;
        contact.Update(
            request.Request.ContactName,
            request.Request.Phone,
            request.Request.PhoneType,
            request.Request.Wechat,
            request.Request.RoleName,
            request.Request.IsPrimary,
            request.Request.Remark);

        await ApplyPrimaryChange(target, subject, contact, wasPrimary, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await PublishScoreIfNeeded(subject, cancellationToken);

        return true;
    }

    private async Task<CrmContact> GetContact(
        CrmContactTarget target,
        Guid contactId,
        CancellationToken cancellationToken)
    {
        var contact = await _dbContext.CrmContacts.FirstOrDefaultAsync(
            item =>
                !item.IsDeleted &&
                item.Id == contactId &&
                item.EntityType == target.EntityType &&
                item.EntityId == target.EntityId,
            cancellationToken);

        return contact ?? throw new BusinessException(404, "联系人不存在");
    }

    private async Task<CrmHerbBaseSubject> GetSubject(Guid subjectId, CancellationToken cancellationToken)
    {
        var subject = await _dbContext.CrmHerbBaseSubjects
            .FirstOrDefaultAsync(item => item.Id == subjectId && !item.IsDeleted, cancellationToken);

        return subject ?? throw new BusinessException(404, "药材基地主体不存在");
    }

    private async Task EnsureVendorExists(Guid vendorId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.CrmVendors
            .AnyAsync(item => item.Id == vendorId && !item.IsDeleted, cancellationToken);

        if (!exists)
        {
            throw new BusinessException(404, "厂商不存在");
        }
    }

    private static void EnsureCanSetPrimary(CrmContactUpdateRequest request, CrmContact contact)
    {
        if (request.IsPrimary && contact.Status == InvalidStatus)
        {
            throw new BusinessException(400, "无效联系人不能设为主联系人");
        }
    }

    private async Task EnsurePhoneNotDuplicated(
        CrmContactTarget target,
        Guid contactId,
        string phone,
        CancellationToken cancellationToken)
    {
        var normalizedPhone = phone?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            return;
        }

        var duplicated = await _dbContext.CrmContacts.AnyAsync(
            item =>
                !item.IsDeleted &&
                item.EntityType == target.EntityType &&
                item.EntityId == target.EntityId &&
                item.Id != contactId &&
                item.Phone == normalizedPhone,
            cancellationToken);

        if (duplicated)
        {
            throw new BusinessException(400, "该对象下已存在相同联系电话");
        }
    }

    private async Task ApplyPrimaryChange(
        CrmContactTarget target,
        CrmHerbBaseSubject? subject,
        CrmContact contact,
        bool wasPrimary,
        CancellationToken cancellationToken)
    {
        if (contact.IsPrimary)
        {
            await UnmarkSiblingPrimaryContacts(target, contact.Id, cancellationToken);
            subject?.UpdatePrimaryContact(contact.ContactName, contact.Phone);
            return;
        }

        if (wasPrimary)
        {
            await PromoteOldestValidContactOrClear(target, subject, contact.Id, cancellationToken);
        }
    }

    private async Task UnmarkSiblingPrimaryContacts(
        CrmContactTarget target,
        Guid contactId,
        CancellationToken cancellationToken)
    {
        var siblings = await _dbContext.CrmContacts
            .Where(c =>
                !c.IsDeleted &&
                c.EntityType == target.EntityType &&
                c.EntityId == target.EntityId &&
                c.Id != contactId &&
                c.IsPrimary)
            .ToListAsync(cancellationToken);

        foreach (var sibling in siblings)
        {
            sibling.UnmarkPrimary();
        }
    }

    private async Task PromoteOldestValidContactOrClear(
        CrmContactTarget target,
        CrmHerbBaseSubject? subject,
        Guid excludedContactId,
        CancellationToken cancellationToken)
    {
        var replacement = await _dbContext.CrmContacts
            .Where(c =>
                !c.IsDeleted &&
                c.EntityType == target.EntityType &&
                c.EntityId == target.EntityId &&
                c.Id != excludedContactId &&
                c.Status != InvalidStatus)
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (replacement == null)
        {
            subject?.ClearPrimaryContact();
            return;
        }

        replacement.MarkPrimary();
        subject?.UpdatePrimaryContact(replacement.ContactName, replacement.Phone);
    }

    private async Task PublishScoreIfNeeded(CrmHerbBaseSubject? subject, CancellationToken cancellationToken)
    {
        if (subject != null)
        {
            await _dispatcher.PublishAsync(new CrmHerbBaseSubjectScoreAffectedEvent(subject.Id), cancellationToken);
        }
    }
}
