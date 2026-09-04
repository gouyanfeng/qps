using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Contracts.Crm.CrmVendors;
using QPS.Application.Extensions;
using QPS.Application.Interfaces;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmVendors;

public class GetCrmVendorDemandsQuery : PaginationRequest, IRequest<PaginationResponse<CrmVendorDemandDto>>
{
    public Guid? Id { get; set; }
    public Guid? VendorId { get; set; }
    public Guid? OwnerUserId { get; set; }
    public string? Status { get; set; }
    public string? ProductName { get; set; }
    public string? Keyword { get; set; }
    public DateTime? DemandAtFrom { get; set; }
    public DateTime? DemandAtTo { get; set; }
    public DateTime? ExpectedDeliveryAtFrom { get; set; }
    public DateTime? ExpectedDeliveryAtTo { get; set; }
}
public class GetCrmVendorDemandsHandler : IRequestHandler<GetCrmVendorDemandsQuery, PaginationResponse<CrmVendorDemandDto>>
{
    private readonly IDbContext _dbContext;

    public GetCrmVendorDemandsHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<CrmVendorDemandDto>> Handle(
        GetCrmVendorDemandsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.VendorId.HasValue && !await _dbContext.CrmVendors.AnyAsync(vendor => vendor.Id == request.VendorId && !vendor.IsDeleted, cancellationToken))
        {
            throw new BusinessException(404, "厂商不存在");
        }

        var page = Math.Max(request.Page, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _dbContext.CrmVendorDemands.Where(plan => !plan.IsDeleted);
        if (request.Id.HasValue) query = query.Where(plan => plan.Id == request.Id);
        if (request.VendorId.HasValue) query = query.Where(plan => plan.VendorId == request.VendorId);
        if (request.OwnerUserId.HasValue) query = query.Where(plan => plan.Vendor!.OwnerUserId == request.OwnerUserId);
        if (!string.IsNullOrWhiteSpace(request.Status)) query = query.Where(plan => plan.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.ProductName)) query = query.Where(plan => plan.Items.Any(item => item.ProductName == request.ProductName));
        if (!string.IsNullOrWhiteSpace(request.Keyword)) query = query.Where(plan => plan.DemandNo.Contains(request.Keyword) || plan.DemandName.Contains(request.Keyword) || plan.Vendor!.VendorName.Contains(request.Keyword));
        if (request.DemandAtFrom.HasValue) query = query.Where(plan => plan.DemandAt >= request.DemandAtFrom);
        if (request.DemandAtTo.HasValue) query = query.Where(plan => plan.DemandAt <= request.DemandAtTo);
        if (request.ExpectedDeliveryAtFrom.HasValue) query = query.Where(plan => plan.ExpectedDeliveryAt >= request.ExpectedDeliveryAtFrom);
        if (request.ExpectedDeliveryAtTo.HasValue) query = query.Where(plan => plan.ExpectedDeliveryAt <= request.ExpectedDeliveryAtTo);

        var totalCount = await query.CountAsync(cancellationToken);

        var isAscending = string.Equals(request.SortDirection, "Ascending", StringComparison.OrdinalIgnoreCase);
        var orderedQuery = request.SortField switch
        {
            nameof(CrmVendorDemandDto.DemandName) => isAscending
                ? query.OrderBy(plan => plan.DemandName).ThenByDescending(plan => plan.CreatedAt)
                : query.OrderByDescending(plan => plan.DemandName).ThenByDescending(plan => plan.CreatedAt),
            nameof(CrmVendorDemandDto.CreatedAt) => isAscending
                ? query.OrderBy(plan => plan.CreatedAt)
                : query.OrderByDescending(plan => plan.CreatedAt),
            nameof(CrmVendorDemandDto.UpdatedAt) => isAscending
                ? query.OrderBy(plan => plan.UpdatedAt)
                : query.OrderByDescending(plan => plan.UpdatedAt),
            _ => isAscending
                ? query.OrderBy(plan => plan.DemandAt).ThenBy(plan => plan.CreatedAt)
                : query.OrderByDescending(plan => plan.DemandAt).ThenByDescending(plan => plan.CreatedAt)
        };

        var demandIds = await orderedQuery
            .Select(plan => plan.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var plans = await _dbContext.CrmVendorDemands
            .Where(plan => demandIds.Contains(plan.Id))
            .Select(plan => new CrmVendorDemandDto
            {
                Id = plan.Id,
                VendorId = plan.VendorId,
                DemandNo = plan.DemandNo,
                DemandName = plan.DemandName,
                DemandAt = plan.DemandAt,
                Status = plan.Status,
                SourceType = plan.SourceType,
                ContactId = plan.ContactId,
                ContactName = string.Empty,
                OwnerUserName = string.Empty,
                ExpectedDeliveryAt = plan.ExpectedDeliveryAt,
                ReceivingAddress = plan.ReceivingAddress,
                SourceUrl = plan.SourceUrl,
                Remark = plan.Remark,
                ClosedReason = plan.ClosedReason,
                CreatedAt = plan.CreatedAt,
                UpdatedAt = plan.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        plans = demandIds
            .Select(id => plans.First(plan => plan.Id == id))
            .ToList();

        var planIds = plans.Select(plan => plan.Id).ToList();
        var items = await _dbContext.CrmVendorDemandItems
            .Where(item => planIds.Contains(item.VendorDemandId))
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);
        foreach (var plan in plans)
        {
            plan.Items = items.Where(item => item.VendorDemandId == plan.Id).Select(item => new CrmVendorDemandItemDto { Id = item.Id, ProductName = item.ProductName, Quantity = item.Quantity, QuantityUnit = item.QuantityUnit, Specification = item.Specification, QualityRequirement = item.QualityRequirement, TargetPrice = item.TargetPrice, PriceUnit = item.PriceUnit, ExpectedDeliveryAt = item.ExpectedDeliveryAt, Remark = item.Remark, SortOrder = item.SortOrder }).ToList();
        }

        return new PaginationResponse<CrmVendorDemandDto>(plans, totalCount, page, pageSize);
    }
}
