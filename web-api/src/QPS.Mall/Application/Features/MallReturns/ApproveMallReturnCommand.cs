using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallReturns;

public class ApproveMallReturnCommand : IRequest<MallReturnDto>
{
    public Guid? ReturnId { get; set; }

    public string? ReturnNo { get; set; }

    public bool Approve { get; set; }

    /// <summary>审核拒绝时填写的原因。</summary>
    public string? RejectReason { get; set; }
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
        var returnEntity = await MallReturnMapper.ResolveAsync(
            _dbContext,
            command.ReturnId,
            command.ReturnNo,
            cancellationToken);

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
            // 拒绝退货时回到退货前真实状态：已退款待入库 → Refunded，已签收 → Completed，否则 → Shipped
            var previousStatus = MallOrderResolver.StatusBeforeReturn(order);

            order.FailReturn(previousStatus);
            returnEntity.Reject(command.RejectReason);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MallReturnMapper.ToDto(returnEntity);
    }
}