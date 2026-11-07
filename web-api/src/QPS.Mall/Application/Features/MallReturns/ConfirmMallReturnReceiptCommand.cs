using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallCoupons;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallReturns;

public class ConfirmMallReturnReceiptCommand : IRequest<MallReturnDto>
{
    public Guid? ReturnId { get; set; }

    public string? ReturnNo { get; set; }
}

public class ConfirmMallReturnReceiptHandler : IRequestHandler<ConfirmMallReturnReceiptCommand, MallReturnDto>
{
    private readonly IMallDbContext _dbContext;

    public ConfirmMallReturnReceiptHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallReturnDto> Handle(
        ConfirmMallReturnReceiptCommand command,
        CancellationToken cancellationToken)
    {
        var returnEntity = await MallReturnMapper.ResolveAsync(
            _dbContext,
            command.ReturnId,
            command.ReturnNo,
            cancellationToken);

        if (returnEntity.Status != MallReturnStatus.Approved)
        {
            throw new BusinessException(400, "仅审核通过的退货单可确认收货");
        }

        var order = await _dbContext.MallOrders
            .FirstOrDefaultAsync(o => o.Id == returnEntity.OrderId, cancellationToken);

        if (order is null)
        {
            throw new BusinessException(404, "关联订单不存在");
        }

        returnEntity.MarkReceived();

        // 退货退款同样按整单实付金额执行，退货单金额与订单实付不一致时拒绝
        if (Math.Round(returnEntity.Amount, 2) != Math.Round(order.PaidAmount, 2))
        {
            throw new BusinessException(400, "退货单金额与订单实付金额不一致，无法退款");
        }

        // 已发货订单先退款（款已退、库存待入库）后再确认收货时，不再重复建退款单，只回补库存
        var alreadyRefunded = order.PaidAmount > 0 && order.RefundedAmount >= order.PaidAmount;

        // 事务由 MediatR 事务行为统一包裹：建退款单、状态更新与回补流水同事务提交/回滚
        Guid refundId;

        if (alreadyRefunded)
        {
            var existingRefundId = await _dbContext.MallRefunds
                .AsNoTracking()
                .Where(r => r.OrderId == order.Id && r.Status == MallRefundStatus.Success)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingRefundId == Guid.Empty)
            {
                throw new BusinessException(400, "订单已退款但未找到成功退款单");
            }

            refundId = existingRefundId;
        }
        else
        {
            var refund = MallRefund.Create(
                MallNumberGenerator.Generate("REF"),
                returnEntity.OrderId,
                returnEntity.PaymentId,
                order.PaidAmount,
                "退货退款");

            refund.MarkSuccess();

            _dbContext.MallRefunds.Add(refund);

            refundId = refund.Id;

            // 支付状态同步：退货退款成功后，该订单所有成功支付单转为已退款
            var successfulPayments = await _dbContext.MallPayments
                .Where(item =>
                    item.OrderId == order.Id &&
                    item.Status == MallPaymentStatus.Success)
                .ToListAsync(cancellationToken);

            foreach (var payment in successfulPayments)
            {
                payment.MarkRefunded();
            }
        }

        order.CompleteReturn();

        // 退货单状态流转由领域方法唯一写入；重复调用会因状态校验失败而拒绝
        returnEntity.MarkCompleted(refundId);

        // 回补只做一次：以退款/退货回补流水为幂等标记；赠品行随明细一并回补
        await MallOrderResolver.RestoreStockOnceAsync(
            _dbContext,
            order,
            MallStockChangeType.ReturnRestore,
            cancellationToken);

        await MallCouponService.ReleaseByOrderNoAsync(_dbContext, order.OrderNo, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MallReturnMapper.ToDto(returnEntity);
    }
}