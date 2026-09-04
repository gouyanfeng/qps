using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;
using QPS.Domain.Events.Crm;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmContacts;

public class SetPrimaryCrmContactCommand : IRequest<bool>
{
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public Guid Id { get; set; }
}

public class SetPrimaryCrmContactHandler : IRequestHandler<SetPrimaryCrmContactCommand, bool>
{
    private const string InvalidStatus = "无效";

    private readonly IDbContext _dbContext;
    private readonly IDomainEventDispatcher _dispatcher;

    public SetPrimaryCrmContactHandler(IDbContext dbContext, IDomainEventDispatcher dispatcher)
    {
        _dbContext = dbContext;
        _dispatcher = dispatcher;
    }

    public async Task<bool> Handle(SetPrimaryCrmContactCommand request, CancellationToken cancellationToken)
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
        EnsureContactCanBePrimary(contact);

        await UnmarkSiblingPrimaryContacts(target, contact.Id, cancellationToken);
        contact.MarkPrimary();
        subject?.UpdatePrimaryContact(contact.ContactName, contact.Phone);

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

    private static void EnsureContactCanBePrimary(CrmContact contact)
    {
        if (contact.Status == InvalidStatus)
        {
            throw new BusinessException(400, "无效联系人不能设为主联系人");
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

    private async Task PublishScoreIfNeeded(CrmHerbBaseSubject? subject, CancellationToken cancellationToken)
    {
        if (subject != null)
        {
            await _dispatcher.PublishAsync(new CrmHerbBaseSubjectScoreAffectedEvent(subject.Id), cancellationToken);
        }
    }
}
