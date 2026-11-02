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
/// 申请整单退款，仅已支付未发货订单可发起。
/// </summary>
public class CreateMallRefundCommand : IRequest<MallRefundDto>
{
    /// <summary>管理端订单主键。</summary>
    public Guid? OrderId { get; set; }

    /// <summary>外部订单号。</summary>
    public string? OrderNo { get; set; }

    public CreateMallRefundRequest Request { get; set; } = new();
}

public class CreateMallRefundHandler : IRequestHandler<CreateMallRefundCommand, MallRefundDto>
{
    private const int ReasonMaxLength = 200;

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

        if (order.Status != MallOrderStatus.Paid)
        {
            throw new BusinessException(400, "仅已支付未发货订单可以申请退款");
        }

        var payment = await _dbContext.MallPayments
            .AsNoTracking()
            .Where(item =>
                item.OrderId == order.Id &&
                item.Status == MallPaymentStatus.Success &&
                !item.IsDeleted)
            .Select(item => new { item.Id, item.Amount })
            .FirstOrDefaultAsync(cancellationToken);

        if (payment is null)
        {
            throw new BusinessException(400, "未找到成功支付单，无法退款");
        }

        var reason = command.Request.Reason?.Trim() ?? string.Empty;

        if (reason.Length > ReasonMaxLength)
        {
            throw new BusinessException(400, $"退款原因不能超过 {ReasonMaxLength} 个字符");
        }

        order.BeginRefund();

        var refund = MallRefund.Create(
            MallNumberGenerator.Generate("REF"),
            order.Id,
            payment.Id,
            payment.Amount,
            reason);

        _dbContext.MallRefunds.Add(refund);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MallRefundDto
        {
            Id = refund.Id,
            RefundNo = refund.RefundNo,
            PaymentId = refund.PaymentId,
            Amount = refund.Amount,
            Reason = refund.Reason,
            Status = refund.Status,
            RefundedAt = refund.RefundedAt,
            CreatedAt = refund.CreatedAt
        };
    }
}
