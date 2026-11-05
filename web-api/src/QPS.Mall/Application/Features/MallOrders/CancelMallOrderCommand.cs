using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Domain;

namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>
/// 取消待支付订单，回补库存并关闭未决支付单。
/// </summary>
public class CancelMallOrderCommand : IRequest<bool>
{
    /// <summary>管理端订单主键。</summary>
    public Guid? OrderId { get; set; }

    /// <summary>外部订单号。</summary>
    public string? OrderNo { get; set; }
}

public class CancelMallOrderHandler : IRequestHandler<CancelMallOrderCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public CancelMallOrderHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(CancelMallOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await MallOrderResolver.ResolveAsync(
            _dbContext,
            request.OrderId,
            request.OrderNo,
            cancellationToken);

        order.Cancel();

        await MallOrderResolver.RestoreStockAsync(
            _dbContext,
            order,
            MallStockChangeType.OrderCancel,
            cancellationToken);

        await MallOrderResolver.ReleasePromotionUsageAsync(
            _dbContext,
            order,
            null,
            cancellationToken);

        var pendingPayments = await _dbContext.MallPayments
            .Where(payment =>
                payment.OrderId == order.Id &&
                payment.Status == MallPaymentStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (var payment in pendingPayments)
        {
            payment.Cancel();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}