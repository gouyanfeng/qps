using MediatR;
using QPS.Crm.Contracts;
using QPS.Crm.Contracts.DTOs.CrmTransfers;

namespace QPS.Crm.Application.Features.CrmTransfers;

public sealed class GetCrmTransferRecordsQuery : IRequest<List<CrmTransferRecordDto>>
{
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }
}

/// <summary>
/// 查询业务实体的负责人转交记录，统一经由公共 Pipeline。
/// </summary>
public sealed class GetCrmTransferRecordsHandler(ICrmDbContext dbContext)
    : IRequestHandler<GetCrmTransferRecordsQuery, List<CrmTransferRecordDto>>
{
    public Task<List<CrmTransferRecordDto>> Handle(
        GetCrmTransferRecordsQuery request,
        CancellationToken cancellationToken)
    {
        return CrmTransferRecordQuery.GetAsync(
            dbContext,
            request.EntityType,
            request.EntityId,
            cancellationToken);
    }
}
