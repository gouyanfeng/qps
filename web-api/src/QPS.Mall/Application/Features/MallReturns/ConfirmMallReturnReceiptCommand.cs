using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Contracts;
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
        var returnEntity = await ResolveReturnAsync(command, cancellationToken);

        if (returnEntity.Status != MallReturnStatus.Approved)
        {
            throw new BusinessException(400, "仅审核通过的退货单可确认收货");
        }

        var order = await _dbContext.MallOrders
            .FirstOrDefaultAsync(o => o.Id == returnEntity.OrderId && !o.IsDeleted, cancellationToken);

        if (order is null)
        {
            throw new BusinessException(404, "关联订单不存在");
        }

        returnEntity.MarkReceived();

        var refund = MallRefund.Create(
            MallNumberGenerator.Generate("REF"),
            returnEntity.OrderId,
            returnEntity.PaymentId,
            returnEntity.Amount,
            "退货退款");

        refund.MarkSuccess();

        order.CompleteReturn();

        _dbContext.MallRefunds.Add(refund);

        returnEntity.MarkCompleted(refund.Id);

        await MallOrderResolver.RestoreStockAsync(
            _dbContext,
            order,
            MallStockChangeType.ReturnRestore,
            cancellationToken);

        await MallOrderResolver.ReleasePromotionUsageAsync(
            _dbContext,
            order,
            null,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(returnEntity);
    }

    private async Task<MallReturn> ResolveReturnAsync(
        ConfirmMallReturnReceiptCommand command,
        CancellationToken cancellationToken)
    {
        MallReturn? r;

        if (command.ReturnId.HasValue)
        {
            r = await _dbContext.MallReturns
                .FirstOrDefaultAsync(x => x.Id == command.ReturnId.Value && !x.IsDeleted, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(command.ReturnNo))
        {
            var normalized = command.ReturnNo.Trim();
            r = await _dbContext.MallReturns
                .FirstOrDefaultAsync(x => x.ReturnNo == normalized && !x.IsDeleted, cancellationToken);
        }
        else
        {
            throw new BusinessException(400, "退货单标识不能为空");
        }

        if (r is null)
        {
            throw new BusinessException(404, "退货单不存在");
        }

        return r;
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