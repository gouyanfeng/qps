using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Common;
using QPS.Shared.Extensions;

namespace QPS.Mall.Application.Features.MallShipments;

/// <summary>
/// 管理端分页查询发货单（运单），包含已作废记录。
/// </summary>
public class GetMallShipmentsQuery : PaginationRequest, IRequest<PaginationResponse<MallShipmentListDto>>
{
    public GetMallShipmentsQuery()
    {
        SortField = nameof(MallShipmentListDto.ShippedAt);
        SortDirection = "Descending";
    }

    /// <summary>关键字，匹配订单号或快递单号。</summary>
    public string? Keyword { get; set; }

    /// <summary>发货开始时间。</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>发货结束时间。</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>是否作废，为空时返回全部记录。</summary>
    public bool? IsVoided { get; set; }

    /// <summary>运单状态（Shipped/Delivered），为空时不按状态过滤。</summary>
    public string? Status { get; set; }
}

public class GetMallShipmentsHandler
    : IRequestHandler<GetMallShipmentsQuery, PaginationResponse<MallShipmentListDto>>
{
    private readonly IMallDbContext _dbContext;

    public GetMallShipmentsHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginationResponse<MallShipmentListDto>> Handle(
        GetMallShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var query =
            from shipment in _dbContext.MallShipments.AsNoTracking()
            join order in _dbContext.MallOrders.AsNoTracking() on shipment.OrderId equals order.Id
            select new { shipment, order };

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(row =>
                row.order.OrderNo.Contains(keyword) ||
                row.shipment.TrackingNo.Contains(keyword));
        }

        if (request.StartTime.HasValue)
        {
            query = query.Where(row => row.shipment.ShippedAt >= request.StartTime.Value);
        }

        if (request.EndTime.HasValue)
        {
            query = query.Where(row => row.shipment.ShippedAt <= request.EndTime.Value);
        }

        if (request.IsVoided.HasValue)
        {
            query = query.Where(row => row.shipment.IsVoided == request.IsVoided.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            query = query.Where(row => row.shipment.Status == status);
        }

        var dtoQuery = query.Select(row => new MallShipmentListDto
        {
            Id = row.shipment.Id,
            ShipmentNo = row.shipment.ShipmentNo,
            OrderId = row.shipment.OrderId,
            OrderNo = row.order.OrderNo,
            ExpressCompany = row.shipment.ExpressCompany,
            TrackingNo = row.shipment.TrackingNo,
            Status = row.shipment.Status,
            IsVoided = row.shipment.IsVoided,
            Remark = row.shipment.Remark,
            ShippedAt = row.shipment.ShippedAt,
            DeliveredAt = row.shipment.DeliveredAt,
            VoidedAt = row.shipment.VoidedAt,
            VoidedBy = row.shipment.VoidedBy,
            VoidReason = row.shipment.VoidReason
        });

        return await dtoQuery.ToPaginationResponseAsync(request, cancellationToken);
    }
}
