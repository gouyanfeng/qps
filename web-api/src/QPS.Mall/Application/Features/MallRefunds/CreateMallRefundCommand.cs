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
/// 申请整单退款：已支付、已发货、已签收订单均可发起，退款金额 = 订单实付金额（含运费），不支持部分退款。
/// </summary>
public class CreateMallRefundCommand : IRequest<MallRefundDto>
{
    /// <summary>管理端订单主键。</summary>
    public Guid? OrderId { get; set; }

    /// <summary>外部订单号。</summary>
    public string? OrderNo { get; set; }

    /// <summary>当前会员 ID；移动端必传，管理端为空。</summary>
    public Guid? MemberId { get; set; }

    public CreateMallRefundRequest Request { get; set; } = new();
}

public class CreateMallRefundHandler : IRequestHandler<CreateMallRefundCommand, MallRefundDto>
{
    private readonly IMallDbContext _dbContext;

    public CreateMallRefundHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallRefundDto> Handle(
        CreateMallRefundCommand command,
        CancellationToken cancellationToken)
    {
        var order = await MallOrderResolver.ResolveAsync(
            _dbContext,
            command.OrderId,
            command.OrderNo,
            cancellationToken);

        MallOrderResolver.EnsureOwnership(order, command.MemberId);

        // 已签收订单需在签收后 7 天内申请；待发货订单的仅退款不受时效限制
        MallAfterSaleRules.EnsureWithinAfterSaleWindow(order, DateTime.Now);

        // 退款金额只取订单实付金额（含运费），前端传入金额只做一致性校验
        if (order.PaidAmount <= 0)
        {
            throw new BusinessException(400, "订单实付金额异常，无法退款");
        }

        if (command.Request.Amount.HasValue &&
            Math.Round(command.Request.Amount.Value, 2) != Math.Round(order.PaidAmount, 2))
        {
            throw new BusinessException(
                400,
                $"仅支持整单退款，退款金额必须等于订单实付金额 ¥{order.PaidAmount:0.00}");
        }

        // 申请原因必填，凭证图片最多 6 张
        var reason = MallAfterSaleRules.RequireReason(command.Request.Reason);
        var description = MallAfterSaleRules.NormalizeDescription(command.Request.Description);
        var evidenceImages = MallAfterSaleRules.NormalizeEvidence(command.Request.EvidenceImages);

        // 退款资格由领域方法唯一判定（仅已支付/已发货/已完成且未退款订单可进入退款）；
        // 事务由 MediatR 事务行为统一包裹，建单失败时订单状态随事务回滚。
        order.BeginRefund();

        // 取最新成功支付单，未找到则不占用退款资格
        var payment = await _dbContext.MallPayments
            .AsNoTracking()
            .Where(item =>
                item.OrderId == order.Id &&
                item.Status == MallPaymentStatus.Success)
            .OrderByDescending(item => item.PaidAt)
            .ThenByDescending(item => item.CreatedAt)
            .Select(item => new { item.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (payment is null)
        {
            throw new BusinessException(400, "未找到成功支付单，无法退款");
        }

        var refund = MallRefund.Create(
            MallNumberGenerator.Generate("REF"),
            order.Id,
            payment.Id,
            order.PaidAmount,
            reason,
            description,
            evidenceImages);

        _dbContext.MallRefunds.Add(refund);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MallRefundDto
        {
            Id = refund.Id,
            RefundNo = refund.RefundNo,
            PaymentId = refund.PaymentId,
            Amount = refund.Amount,
            Reason = refund.Reason,
            Description = refund.Description,
            EvidenceImages = MallAfterSaleRules.SplitEvidence(refund.EvidenceImages),
            Status = refund.Status,
            RefundedAt = refund.RefundedAt,
            CreatedAt = refund.CreatedAt
        };
    }
}
