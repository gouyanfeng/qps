using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Contracts;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallReturns;

public class CreateMallReturnCommand : IRequest<MallReturnDto>
{
    public Guid? OrderId { get; set; }

    public string? OrderNo { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class CreateMallReturnHandler : IRequestHandler<CreateMallReturnCommand, MallReturnDto>
{
    private const int ReasonMaxLength = 200;

    private readonly IMallDbContext _dbContext;

    public CreateMallReturnHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallReturnDto> Handle(
        CreateMallReturnCommand command,
        CancellationToken cancellationToken)
    {
        var order = await MallOrderResolver.ResolveAsync(
            _dbContext,
            command.OrderId,
            command.OrderNo,
            cancellationToken);

        if (order.Status != MallOrderStatus.Shipped && order.Status != MallOrderStatus.Completed)
        {
            throw new BusinessException(400, "仅已发货或已完成订单可以申请退货");
        }

        var existingReturn = await _dbContext.MallReturns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrderId == order.Id && !r.IsDeleted, cancellationToken);

        if (existingReturn != null && existingReturn.Status != MallReturnStatus.Rejected)
        {
            throw new BusinessException(400, "该订单已存在退货申请");
        }

        var payment = await _dbContext.MallPayments
            .AsNoTracking()
            .Where(p => p.OrderId == order.Id && p.Status == MallPaymentStatus.Success && !p.IsDeleted)
            .Select(p => new { p.Id, p.Amount })
            .FirstOrDefaultAsync(cancellationToken);

        if (payment is null)
        {
            throw new BusinessException(400, "未找到成功支付单，无法申请退货");
        }

        var reason = command.Reason?.Trim() ?? string.Empty;

        if (reason.Length > ReasonMaxLength)
        {
            throw new BusinessException(400, $"退货原因不能超过 {ReasonMaxLength} 个字符");
        }

        order.BeginReturn();

        var returnEntity = MallReturn.Create(
            MallNumberGenerator.Generate("RET"),
            order.Id,
            payment.Id,
            payment.Amount,
            reason);

        _dbContext.MallReturns.Add(returnEntity);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(returnEntity);
    }

    private static MallReturnDto ToDto(MallReturn r)
    {
        return new MallReturnDto
        {
            Id = r.Id,
            ReturnNo = r.ReturnNo,
            OrderId = r.OrderId,
            PaymentId = r.PaymentId,
            RefundId = r.RefundId,
            Amount = r.Amount,
            Reason = r.Reason,
            Status = r.Status,
            ExpressCompany = r.ExpressCompany,
            TrackingNo = r.TrackingNo,
            ApprovedAt = r.ApprovedAt,
            RejectedAt = r.RejectedAt,
            ShippedAt = r.ShippedAt,
            ReceivedAt = r.ReceivedAt,
            CompletedAt = r.CompletedAt,
            CreatedAt = r.CreatedAt
        };
    }
}

public class MallReturnDto
{
    public Guid Id { get; set; }
    public string ReturnNo { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public Guid? RefundId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ExpressCompany { get; set; }
    public string? TrackingNo { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}