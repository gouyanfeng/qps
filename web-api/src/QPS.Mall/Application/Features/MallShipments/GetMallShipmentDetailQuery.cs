using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallShipments;

/// <summary>
/// 管理端查询发货单详情。
/// </summary>
public class GetMallShipmentDetailQuery : IRequest<MallShipmentDetailDto>
{
    public Guid Id { get; set; }
}

public class GetMallShipmentDetailHandler : IRequestHandler<GetMallShipmentDetailQuery, MallShipmentDetailDto>
{
    private readonly IMallDbContext _dbContext;

    public GetMallShipmentDetailHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallShipmentDetailDto> Handle(
        GetMallShipmentDetailQuery request,
        CancellationToken cancellationToken)
    {
        var shipment = await _dbContext.MallShipments
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (shipment is null)
        {
            throw new BusinessException(404, "发货单不存在");
        }

        var order = await _dbContext.MallOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == shipment.OrderId, cancellationToken);

        if (order is null)
        {
            throw new BusinessException(404, "发货单关联订单不存在");
        }

        var memberName = string.Empty;
        if (order.MemberId.HasValue)
        {
            var member = await _dbContext.MallMembers
                .AsNoTracking()
                .Where(item => item.Id == order.MemberId.Value)
                .Select(item => new { item.Nickname, item.Phone })
                .FirstOrDefaultAsync(cancellationToken);
            if (member is not null)
            {
                memberName = string.IsNullOrWhiteSpace(member.Nickname) ? member.Phone : member.Nickname;
            }
        }

        var itemCount = await _dbContext.MallOrderItems
            .AsNoTracking()
            .Where(item => item.OrderId == order.Id)
            .SumAsync(item => item.Quantity, cancellationToken);

        return new MallShipmentDetailDto
        {
            Id = shipment.Id,
            ShipmentNo = shipment.ShipmentNo,
            OrderId = shipment.OrderId,
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
            OrderStatus = order.Status,
            MemberName = memberName,
            ReceiverName = order.ReceiverName,
            ReceiverPhone = order.ReceiverPhone,
            PaidAmount = order.PaidAmount,
            ItemCount = itemCount
        };
    }
}
