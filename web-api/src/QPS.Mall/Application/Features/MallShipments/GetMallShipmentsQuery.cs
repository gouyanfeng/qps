using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Shared.Common;

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

    /// <summary>关键字，匹配订单号、发货单号、运单号、收货人或电话。</summary>
    public string? Keyword { get; set; }

    /// <summary>发货开始时间；附带待发货订单时按最近状态时间（发货/付款/下单）过滤。</summary>
    public DateTime? StartTime { get; set; }

    /// <summary>发货结束时间；附带待发货订单时按最近状态时间过滤。</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>是否作废，为空时返回全部记录。</summary>
    public bool? IsVoided { get; set; }

    /// <summary>运单状态（Shipped/Delivered），为空时不按状态过滤。</summary>
    public string? Status { get; set; }

    /// <summary>
    /// 是否附带尚未发货的已支付订单（「全部」页签需要）。
    /// 待发货订单还没有运单，只查运单列表会整批漏掉它们。
    /// </summary>
    public bool IncludePendingOrders { get; set; }
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
        // 运单行：一单可能有多张运单（作废后重发），每张运单各占一行
        var shipmentRows =
            from shipment in _dbContext.MallShipments.AsNoTracking()
            join order in _dbContext.MallOrders.AsNoTracking() on shipment.OrderId equals order.Id
            select new ShipmentRow
            {
                Id = shipment.Id,
                ShipmentNo = shipment.ShipmentNo,
                OrderId = order.Id,
                OrderNo = order.OrderNo,
                ExpressCompany = shipment.ExpressCompany,
                TrackingNo = shipment.TrackingNo,
                Status = shipment.Status,
                IsVoided = shipment.IsVoided,
                Remark = shipment.Remark,
                ShippedAt = shipment.ShippedAt,
                DeliveredAt = shipment.DeliveredAt,
                VoidedAt = shipment.VoidedAt,
                VoidedBy = shipment.VoidedBy,
                VoidReason = shipment.VoidReason,
                PaidAmount = order.PaidAmount,
                PaidAt = order.PaidAt,
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                SortTime = shipment.ShippedAt ?? order.PaidAt ?? order.CreatedAt
            };

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = request.Status.Trim();
            shipmentRows = shipmentRows.Where(row => row.Status == status);
        }

        if (request.IsVoided.HasValue)
        {
            shipmentRows = shipmentRows.Where(row => row.IsVoided == request.IsVoided.Value);
        }

        // 附带待发货订单时，运单行与订单行都按「最近状态时间」过滤，否则待发货订单没有发货时间会被整批过滤掉
        if (request.StartTime.HasValue)
        {
            var startTime = request.StartTime.Value;
            shipmentRows = request.IncludePendingOrders
                ? shipmentRows.Where(row => row.SortTime >= startTime)
                : shipmentRows.Where(row => row.ShippedAt >= startTime);
        }

        if (request.EndTime.HasValue)
        {
            var endTime = request.EndTime.Value;
            shipmentRows = request.IncludePendingOrders
                ? shipmentRows.Where(row => row.SortTime <= endTime)
                : shipmentRows.Where(row => row.ShippedAt <= endTime);
        }

        var keyword = string.IsNullOrWhiteSpace(request.Keyword) ? null : request.Keyword.Trim();
        if (keyword != null)
        {
            shipmentRows = shipmentRows.Where(row =>
                row.OrderNo.Contains(keyword) ||
                row.ShipmentNo.Contains(keyword) ||
                row.TrackingNo.Contains(keyword) ||
                row.ReceiverName.Contains(keyword) ||
                row.ReceiverPhone.Contains(keyword));
        }

        IQueryable<ShipmentRow> rows = shipmentRows;

        if (request.IncludePendingOrders)
        {
            // 待发货行：已支付且没有任何有效运单的订单（运单作废后订单会退回 Paid，需要重新发货）
            var pendingRows =
                from order in _dbContext.MallOrders.AsNoTracking()
                where order.Status == MallOrderStatus.Paid &&
                      !_dbContext.MallShipments.Any(shipment =>
                          shipment.OrderId == order.Id && !shipment.IsVoided)
                select new ShipmentRow
                {
                    Id = order.Id,
                    ShipmentNo = string.Empty,
                    OrderId = order.Id,
                    OrderNo = order.OrderNo,
                    ExpressCompany = string.Empty,
                    TrackingNo = string.Empty,
                    Status = order.Status,
                    IsVoided = false,
                    Remark = string.Empty,
                    ShippedAt = null,
                    DeliveredAt = null,
                    VoidedAt = null,
                    VoidedBy = string.Empty,
                    VoidReason = string.Empty,
                    PaidAmount = order.PaidAmount,
                    PaidAt = order.PaidAt,
                    ReceiverName = order.ReceiverName,
                    ReceiverPhone = order.ReceiverPhone,
                    SortTime = order.PaidAt ?? order.CreatedAt
                };

            if (request.StartTime.HasValue)
            {
                var startTime = request.StartTime.Value;
                pendingRows = pendingRows.Where(row => row.SortTime >= startTime);
            }

            if (request.EndTime.HasValue)
            {
                var endTime = request.EndTime.Value;
                pendingRows = pendingRows.Where(row => row.SortTime <= endTime);
            }

            if (keyword != null)
            {
                pendingRows = pendingRows.Where(row =>
                    row.OrderNo.Contains(keyword) ||
                    row.ReceiverName.Contains(keyword) ||
                    row.ReceiverPhone.Contains(keyword));
            }

            rows = shipmentRows.Concat(pendingRows);
        }

        var totalCount = await rows.CountAsync(cancellationToken);

        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        var list = await rows
            .OrderByDescending(row => row.SortTime)
            .ThenByDescending(row => row.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = list.Select(row => new MallShipmentListDto
        {
            Id = row.Id,
            ShipmentNo = row.ShipmentNo,
            OrderId = row.OrderId,
            OrderNo = row.OrderNo,
            ExpressCompany = row.ExpressCompany,
            TrackingNo = row.TrackingNo,
            Status = row.Status,
            IsVoided = row.IsVoided,
            Remark = row.Remark,
            ShippedAt = row.ShippedAt,
            DeliveredAt = row.DeliveredAt,
            VoidedAt = row.VoidedAt,
            VoidedBy = row.VoidedBy,
            VoidReason = row.VoidReason,
            PaidAmount = row.PaidAmount,
            PaidAt = row.PaidAt
        }).ToList();

        if (dtos.Count > 0)
        {
            var orderIds = dtos.Select(item => item.OrderId).Distinct().ToList();

            // 件数按订单明细批量汇总，避免在分页查询里为每行挂相关子查询
            var itemCountMap = (await _dbContext.MallOrderItems
                    .AsNoTracking()
                    .Where(item => orderIds.Contains(item.OrderId))
                    .GroupBy(item => item.OrderId)
                    .Select(group => new
                    {
                        OrderId = group.Key,
                        Quantity = group.Sum(item => item.Quantity)
                    })
                    .ToListAsync(cancellationToken))
                .ToDictionary(item => item.OrderId, item => item.Quantity);

            foreach (var dto in dtos)
            {
                dto.ItemCount = itemCountMap.TryGetValue(dto.OrderId, out var quantity) ? quantity : 0;
            }

            // 会员名按订单 MemberId 批量回填，昵称为空时回退手机号
            var memberIdMap = (await _dbContext.MallOrders
                    .AsNoTracking()
                    .Where(order => orderIds.Contains(order.Id) && order.MemberId.HasValue)
                    .Select(order => new { order.Id, order.MemberId })
                    .ToListAsync(cancellationToken))
                .ToDictionary(item => item.Id, item => item.MemberId!.Value);

            var memberIds = memberIdMap.Values.Distinct().ToList();
            if (memberIds.Count > 0)
            {
                var memberNameMap = (await _dbContext.MallMembers
                        .AsNoTracking()
                        .Where(member => memberIds.Contains(member.Id))
                        .Select(member => new { member.Id, member.Nickname, member.Phone })
                        .ToListAsync(cancellationToken))
                    .ToDictionary(
                        member => member.Id,
                        member => string.IsNullOrWhiteSpace(member.Nickname) ? member.Phone : member.Nickname);

                foreach (var dto in dtos)
                {
                    if (memberIdMap.TryGetValue(dto.OrderId, out var memberId) &&
                        memberNameMap.TryGetValue(memberId, out var memberName))
                    {
                        dto.MemberName = memberName;
                    }
                }
            }
        }

        return new PaginationResponse<MallShipmentListDto>(dtos, totalCount, page, pageSize);
    }

    /// <summary>
    /// 运单行与待发货订单行的统一投影，供「全部」页签合并分页。
    /// </summary>
    private class ShipmentRow
    {
        public Guid Id { get; set; }

        public string ShipmentNo { get; set; } = string.Empty;

        public Guid OrderId { get; set; }

        public string OrderNo { get; set; } = string.Empty;

        public string ExpressCompany { get; set; } = string.Empty;

        public string TrackingNo { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public bool IsVoided { get; set; }

        public string Remark { get; set; } = string.Empty;

        public DateTime? ShippedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime? VoidedAt { get; set; }

        public string VoidedBy { get; set; } = string.Empty;

        public string VoidReason { get; set; } = string.Empty;

        public decimal PaidAmount { get; set; }

        public DateTime? PaidAt { get; set; }

        public string ReceiverName { get; set; } = string.Empty;

        public string ReceiverPhone { get; set; } = string.Empty;

        /// <summary>排序时间：发货时间，未发货时退化为付款时间或下单时间。</summary>
        public DateTime SortTime { get; set; }
    }
}