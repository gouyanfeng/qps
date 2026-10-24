using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallPayments;

/// <summary>
/// 模拟支付结果：成功时订单转已支付，失败保持待支付可重试。
/// </summary>
public class SimulateMallPaymentCommand : IRequest<bool>
{
    /// <summary>管理端支付单主键。</summary>
    public Guid? PaymentId { get; set; }

    /// <summary>外部支付单号。</summary>
    public string? PaymentNo { get; set; }

    public SimulateMallPaymentRequest Request { get; set; } = new();
}

public class SimulateMallPaymentHandler : IRequestHandler<SimulateMallPaymentCommand, bool>
{
    private const int FailureReasonMaxLength = 200;

    private readonly IMallDbContext _dbContext;

    public SimulateMallPaymentHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(
        SimulateMallPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var payment = await ResolvePaymentAsync(command, cancellationToken);

        if (payment.Status != MallPaymentStatus.Pending)
        {
            throw new BusinessException(400, "当前支付单状态不可模拟支付");
        }

        if (!command.Request.Success)
        {
            var failureReason = string.IsNullOrWhiteSpace(command.Request.FailureReason)
                ? "模拟支付失败"
                : command.Request.FailureReason.Trim();

            if (failureReason.Length > FailureReasonMaxLength)
            {
                throw new BusinessException(400, $"失败原因不能超过 {FailureReasonMaxLength} 个字符");
            }

            payment.MarkFailed(failureReason);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        var alreadyPaid = await _dbContext.MallPayments.AnyAsync(
            item =>
                item.OrderId == payment.OrderId &&
                item.Status == MallPaymentStatus.Success &&
                item.Id != payment.Id &&
                !item.IsDeleted,
            cancellationToken);

        if (alreadyPaid)
        {
            throw new BusinessException(400, "订单已支付，不能重复支付");
        }

        var order = await _dbContext.MallOrders
            .FirstOrDefaultAsync(item => item.Id == payment.OrderId && !item.IsDeleted, cancellationToken);

        if (order is null)
        {
            throw new BusinessException(404, "订单不存在");
        }

        payment.MarkSuccess(MallNumberGenerator.GenerateTransactionNo());
        order.MarkPaid(payment.Amount);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<MallPayment> ResolvePaymentAsync(
        SimulateMallPaymentCommand command,
        CancellationToken cancellationToken)
    {
        MallPayment? payment;

        if (command.PaymentId.HasValue)
        {
            payment = await _dbContext.MallPayments
                .FirstOrDefaultAsync(
                    item => item.Id == command.PaymentId.Value && !item.IsDeleted,
                    cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(command.PaymentNo))
        {
            var paymentNo = command.PaymentNo.Trim();
            payment = await _dbContext.MallPayments
                .FirstOrDefaultAsync(
                    item => item.PaymentNo == paymentNo && !item.IsDeleted,
                    cancellationToken);
        }
        else
        {
            throw new BusinessException(400, "支付单标识不能为空");
        }

        if (payment is null)
        {
            throw new BusinessException(404, "支付单不存在");
        }

        return payment;
    }
}
