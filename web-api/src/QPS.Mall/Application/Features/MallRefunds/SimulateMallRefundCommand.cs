using MediatR;
using Microsoft.EntityFrameworkCore;
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

    public SimulateMallRefundRequest Request { get; set; } = new();
}

public class SimulateMallRefundHandler : IRequestHandler<SimulateMallRefundCommand, bool>
{
    private readonly IMallDbContext _dbContext;

    public SimulateMallRefundHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(
        SimulateMallRefundCommand command,
        CancellationToken cancellationToken)
    {
        var refund = await ResolveRefundAsync(command, cancellationToken);

        if (refund.Status != MallRefundStatus.Pending)
        {
            throw new BusinessException(400, "当前退款单状态不可模拟退款");
        }

        var order = await _dbContext.MallOrders
            .FirstOrDefaultAsync(item => item.Id == refund.OrderId && !item.IsDeleted, cancellationToken);

        if (order is null)
        {
            throw new BusinessException(404, "订单不存在");
        }

        if (!command.Request.Success)
        {
            refund.MarkFailed();
            order.FailRefund();

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        refund.MarkSuccess();
        order.CompleteRefund();

        await MallOrderResolver.RestoreStockAsync(
            _dbContext,
            order,
            MallStockChangeType.RefundRestore,
            cancellationToken);

        await MallOrderResolver.ReleasePromotionUsageAsync(
            _dbContext,
            order,
            null,
            cancellationToken);

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
                    item => item.Id == command.RefundId.Value && !item.IsDeleted,
                    cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(command.RefundNo))
        {
            var refundNo = command.RefundNo.Trim();
            refund = await _dbContext.MallRefunds
                .FirstOrDefaultAsync(
                    item => item.RefundNo == refundNo && !item.IsDeleted,
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