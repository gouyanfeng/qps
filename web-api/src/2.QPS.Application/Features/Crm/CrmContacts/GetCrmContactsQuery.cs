using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Contracts.Crm.CrmContacts;
using QPS.Application.Features.Crm;
using QPS.Application.Interfaces;

namespace QPS.Application.Features.Crm.CrmContacts;

public class GetCrmContactsQuery : IRequest<List<CrmContactDto>>
{
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }
}

public class GetCrmContactsHandler : IRequestHandler<GetCrmContactsQuery, List<CrmContactDto>>
{
    private readonly IDbContext _dbContext;

    /// <summary>
    /// 查询基地主体联系人处理器。
    /// </summary>
    public GetCrmContactsHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 编排查询客户联系人用例。
    /// </summary>
    public async Task<List<CrmContactDto>> Handle(GetCrmContactsQuery request, CancellationToken cancellationToken)
    {
        var target = new CrmContactTarget(request.EntityType, request.EntityId);
        target.EnsureSupported();
        await EnsureTargetExists(target, cancellationToken);

        return await _dbContext.CrmContacts
            .Where(c => !c.IsDeleted && c.EntityType == target.EntityType && c.EntityId == target.EntityId)
            .OrderByDescending(c => c.IsPrimary)
            .ThenBy(c => c.CreatedAt)
            .Select(c => new CrmContactDto
            {
                Id = c.Id,
                EntityType = c.EntityType,
                EntityId = c.EntityId,
                ContactName = c.ContactName,
                Phone = c.Phone,
                PhoneType = c.PhoneType,
                Wechat = c.Wechat,
                RoleName = c.RoleName,
                IsPrimary = c.IsPrimary,
                Status = c.Status,
                Remark = c.Remark,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private async Task EnsureTargetExists(CrmContactTarget target, CancellationToken cancellationToken)
    {
        var exists = target.EntityType switch
        {
            CrmCodes.HerbBaseSubjectEntityType => await _dbContext.CrmHerbBaseSubjects
                .AnyAsync(subject => subject.Id == target.EntityId && !subject.IsDeleted, cancellationToken),
            CrmCodes.VendorEntityType => await _dbContext.CrmVendors
                .AnyAsync(vendor => vendor.Id == target.EntityId && !vendor.IsDeleted, cancellationToken),
            _ => false
        };

        if (!exists)
        {
            throw new QPS.Domain.Exceptions.BusinessException(
                404,
                target.IsHerbBaseSubject ? "药材基地主体不存在" : "厂商不存在");
        }
    }
}
