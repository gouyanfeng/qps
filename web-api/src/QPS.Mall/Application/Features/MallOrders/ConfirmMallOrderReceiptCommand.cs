using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;

namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>
/// 确认收货：订单完成，运单标记签收。
/// </summary>
public class ConfirmMallOrderReceiptCommand : IRequest<bool>
{
    /// <summary>管理端订单主键。</summary>
    public Guid? OrderId { get; set; }

    /// <summary>外部订单号。</summary>
    public string? OrderNo { get; set; }

    /// <summary>当前会员 ID；移动端必传，管理端为空。</summary>
    public Guid? MemberId { get; set; }
}

public class ConfirmMallOrderReceiptHandler : IRequestHandler<ConfirmMallOrderReceiptCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public ConfirmMallOrderReceiptHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(
        ConfirmMallOrderReceiptCommand request,
        CancellationToken cancellationToken)
    {
        var order = await MallOrderResolver.ResolveAsync(
            _dbContext,
            request.OrderId,
            request.OrderNo,
            cancellationToken);

        MallOrderResolver.EnsureOwnership(order, request.MemberId);

        order.MarkCompleted();

        var shipment = await _dbContext.MallShipments
            .FirstOrDefaultAsync(item => item.OrderId == order.Id && !item.IsVoided, cancellationToken);

        shipment?.MarkDelivered();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
