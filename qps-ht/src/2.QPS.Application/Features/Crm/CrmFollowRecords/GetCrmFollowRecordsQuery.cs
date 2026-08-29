using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Features.Crm;
using QPS.Application.Interfaces;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmFollowRecords;

public class GetCrmFollowRecordsQuery : IRequest<List<CrmFollowRecordDto>>
{
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }
}

public class GetCrmFollowRecordsHandler : IRequestHandler<GetCrmFollowRecordsQuery, List<CrmFollowRecordDto>>
{
    private const string HerbBaseSubjectEntityType = CrmCodes.HerbBaseSubjectEntityType;
    private const string VendorEntityType = CrmCodes.VendorEntityType;
    private readonly IDbContext _dbContext;

    /// <summary>
    /// 查询客户沟通记录处理器。
    /// </summary>
    public GetCrmFollowRecordsHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 查询基地主体或厂商的沟通记录。
    /// </summary>
    public async Task<List<CrmFollowRecordDto>> Handle(GetCrmFollowRecordsQuery request, CancellationToken cancellationToken)
    {
        await EnsureTargetExists(request, cancellationToken);

        return await _dbContext.CrmFollowRecords
            .Where(r => r.EntityType == request.EntityType && r.EntityId == request.EntityId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new CrmFollowRecordDto
            {
                Id = r.Id,
                EntityType = r.EntityType,
                EntityId = r.EntityId,
                ContactId = r.ContactId,
                ContactName = r.Contact != null ? r.Contact.ContactName : null,
                FollowType = r.FollowType,
                FollowResult = r.FollowResult,
                IntentLevel = r.IntentLevel,
                Content = r.Content,
                NextFollowAt = r.NextFollowAt,
                OperatorUserId = r.OperatorUserId,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private async Task EnsureTargetExists(GetCrmFollowRecordsQuery request, CancellationToken cancellationToken)
    {
        var exists = request.EntityType switch
        {
            HerbBaseSubjectEntityType => await _dbContext.CrmHerbBaseSubjects.AnyAsync(subject => subject.Id == request.EntityId, cancellationToken),
            VendorEntityType => await _dbContext.CrmVendors.AnyAsync(vendor => vendor.Id == request.EntityId, cancellationToken),
            _ => throw new BusinessException(400, "不支持的沟通记录对象类型")
        };

        if (!exists)
        {
            throw new BusinessException(404, request.EntityType == HerbBaseSubjectEntityType ? "药材基地主体不存在" : "厂商不存在");
        }
    }
}
