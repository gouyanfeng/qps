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
/// 创建支付单，仅待支付订单可创建。
/// </summary>
public class CreateMallPaymentCommand : IRequest<MallPaymentDto>
{
    /// <summary>管理端订单主键。</summary>
    public Guid? OrderId { get; set; }

    /// <summary>外部订单号。</summary>
    public string? OrderNo { get; set; }

    public CreateMallPaymentRequest Request { get; set; } = new();
}

public class CreateMallPaymentHandler : IRequestHandler<CreateMallPaymentCommand, MallPaymentDto>
{
    private const int MethodMaxLength = 32;

    private readonly IMallDbContext _dbContext;

    public CreateMallPaymentHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallPaymentDto> Handle(
        CreateMallPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var order = await MallOrderResolver.ResolveAsync(
            _dbContext,
            command.OrderId,
            command.OrderNo,
            cancellationToken);

        if (order.Status != MallOrderStatus.PendingPayment)
        {
            throw new BusinessException(400, "当前订单状态不可支付");
        }

        var method = string.IsNullOrWhiteSpace(command.Request.Method)
            ? "Mock"
            : command.Request.Method.Trim();

        if (method.Length > MethodMaxLength)
        {
            throw new BusinessException(400, $"支付方式不能超过 {MethodMaxLength} 个字符");
        }

        var payment = MallPayment.Create(
            MallNumberGenerator.Generate("PAY"),
            order.Id,
            order.PayableAmount,
            method);

        _dbContext.MallPayments.Add(payment);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MallPaymentDto
        {
            Id = payment.Id,
            PaymentNo = payment.PaymentNo,
            Amount = payment.Amount,
            Method = payment.Method,
            Status = payment.Status,
            TransactionNo = payment.TransactionNo,
            FailureReason = payment.FailureReason,
            PaidAt = payment.PaidAt,
            CreatedAt = payment.CreatedAt
        };
    }
}
