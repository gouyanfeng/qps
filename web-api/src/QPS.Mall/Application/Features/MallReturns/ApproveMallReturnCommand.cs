using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallReturns;

public class ApproveMallReturnCommand : IRequest<MallReturnDto>
{
    public Guid? ReturnId { get; set; }

    public string? ReturnNo { get; set; }

    public bool Approve { get; set; }
}

public class ApproveMallReturnHandler : IRequestHandler<ApproveMallReturnCommand, MallReturnDto>
{
    private readonly IMallDbContext _dbContext;

    public ApproveMallReturnHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallReturnDto> Handle(
        ApproveMallReturnCommand command,
        CancellationToken cancellationToken)
    {
        var returnEntity = await ResolveReturnAsync(command, cancellationToken);

        if (returnEntity.Status != MallReturnStatus.Pending)
        {
            throw new BusinessException(400, "当前退货单状态不可审核");
        }

        var order = await _dbContext.MallReturns
            .Where(r => r.Id == returnEntity.Id)
            .Join(_dbContext.MallOrders, r => r.OrderId, o => o.Id, (r, o) => new { Return = r, Order = o })
            .Select(x => x.Order)
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            throw new BusinessException(404, "关联订单不存在");
        }

        if (command.Approve)
        {
            returnEntity.Approve();
        }
        else
        {
            var previousStatus = order.Status == MallOrderStatus.Returning
                ? MallOrderStatus.Shipped
                : MallOrderStatus.Shipped;

            order.FailReturn(previousStatus);
            returnEntity.Reject();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(returnEntity);
    }

    private async Task<MallReturn> ResolveReturnAsync(
        ApproveMallReturnCommand command,
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