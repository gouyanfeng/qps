using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using QPS.Mall.Application.Features.MallCoupons;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallRefunds;

/// <summary>
/// 模拟退款结果：成功时订单转已退款并回补库存，失败回到已支付。
/// </summary>
public class SimulateMallRefundCommand : IRequest<bool>
{
    /// <summary>管理端退款单主键。</summary>
    public Guid? RefundId { get; set; }

    /// <summary>外部退款单号。</summary>
    public string? RefundNo { get; set; }

    /// <summary>当前会员 ID；移动端必传，管理端为空。</summary>
    public Guid? MemberId { get; set; }

    public SimulateMallRefundRequest Request { get; set; } = new();
}

public class SimulateMallRefundHandler : IRequestHandler<SimulateMallRefundCommand, bool>
{
    private readonly IMallDbContext _dbContext;
    private readonly IHostEnvironment _environment;

    public SimulateMallRefundHandler(IMallDbContext dbContext, IHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    public async Task<bool> Handle(
        SimulateMallRefundCommand command,
        CancellationToken cancellationToken)
    {
        if (_environment.IsProduction())
        {
            throw new BusinessException(403, "生产环境不允许模拟退款");
        }

        var refund = await ResolveRefundAsync(command, cancellationToken);

        if (refund.Status != MallRefundStatus.Pending)
        {
            throw new BusinessException(400, "当前退款单状态不可模拟退款");
        }

        var order = await _dbContext.MallOrders
            .FirstOrDefaultAsync(item => item.Id == refund.OrderId, cancellationToken);

        if (order is null)
        {
            throw new BusinessException(404, "订单不存在");
        }

        MallOrderResolver.EnsureOwnership(order, command.MemberId);

        // 整单退款：退款单金额必须等于订单实付金额，防止历史部分退款数据被确认
        if (Math.Round(refund.Amount, 2) != Math.Round(order.PaidAmount, 2))
        {
            throw new BusinessException(400, "退款金额与订单实付金额不一致，请重新发起整单退款");
        }

        // 事务由 MediatR 事务行为统一包裹：状态回退、回补与流水写入同事务提交/回滚
        if (!command.Request.Success)
        {
            // 失败回退：订单状态由领域方法唯一判定。
            // 未发货回到 Paid；已发货/已签收回到发货前状态，不丢失物流状态。
            var fallbackStatus = MallOrderResolver.StatusBeforeRefund(order);

            refund.MarkFailed();
            order.FailRefund(fallbackStatus);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        // 成功：订单状态与退款金额由领域方法唯一写入，避免与条件更新双写
        refund.MarkSuccess();
        order.CompleteRefund();

        // 支付状态同步：整单退款成功后，该订单所有成功支付单转为已退款
        var successfulPayments = await _dbContext.MallPayments
            .Where(item =>
                item.OrderId == order.Id &&
                item.Status == MallPaymentStatus.Success)
            .ToListAsync(cancellationToken);

        foreach (var payment in successfulPayments)
        {
            payment.MarkRefunded();
        }

        // 未发货退款立即回补库存；已发货/已签收退款不回补，等退货入库确认时补。
        // 回补以退款/退货流水为幂等标记，与状态变更处于同一事务。
        if (!MallOrderResolver.WasShipped(order))
        {
            await MallOrderResolver.RestoreStockOnceAsync(
                _dbContext,
                order,
                MallStockChangeType.RefundRestore,
                cancellationToken);
        }

        await MallCouponService.ReleaseByOrderNoAsync(_dbContext, order.OrderNo, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<MallRefund> ResolveRefundAsync(
        SimulateMallRefundCommand command,
        CancellationToken cancellationToken)
    {
        MallRefund? refund;

        if (command.RefundId.HasValue)
        {
            refund = await _dbContext.MallRefunds
                .FirstOrDefaultAsync(
                    item => item.Id == command.RefundId.Value,
                    cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(command.RefundNo))
        {
            var refundNo = command.RefundNo.Trim();
            refund = await _dbContext.MallRefunds
                .FirstOrDefaultAsync(
                    item => item.RefundNo == refundNo,
                    cancellationToken);
        }
        else
        {
            throw new BusinessException(400, "退款单标识不能为空");
        }

        if (refund is null)
        {
            throw new BusinessException(404, "退款单不存在");
        }

        return refund;
    }
}