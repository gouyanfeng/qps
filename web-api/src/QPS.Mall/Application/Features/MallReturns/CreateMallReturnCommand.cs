using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallReturns;

public class CreateMallReturnCommand : IRequest<MallReturnDto>
{
    public Guid? OrderId { get; set; }

    public string? OrderNo { get; set; }

    /// <summary>移动端传当前会员；管理端代客操作时传 null，跳过归属校验。</summary>
    public Guid? MemberId { get; set; }

    public string Reason { get; set; } = string.Empty;

    /// <summary>补充说明，选择「其他」原因时填写。</summary>
    public string? Description { get; set; }

    /// <summary>凭证图片相对地址，最多 6 张。</summary>
    public string[]? EvidenceImages { get; set; }
}

public class CreateMallReturnHandler : IRequestHandler<CreateMallReturnCommand, MallReturnDto>
{
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

        MallOrderResolver.EnsureOwnership(order, command.MemberId);

        // 已签收订单需在签收后 7 天内申请售后
        MallAfterSaleRules.EnsureWithinAfterSaleWindow(order, DateTime.Now);

        // 已退款但库存尚未回补（已发货/已签收后退款）的订单，允许发起退货，等入库确认后回补
        var refundedPendingRestore =
            order.Status == MallOrderStatus.Refunded &&
            MallOrderResolver.WasShipped(order) &&
            !await MallOrderResolver.HasRestoredStockAsync(_dbContext, order, cancellationToken);

        if (order.Status != MallOrderStatus.Shipped &&
            order.Status != MallOrderStatus.Completed &&
            !refundedPendingRestore)
        {
            throw new BusinessException(400, "仅已发货、已完成或已退款待入库订单可以申请退货");
        }

        var existingReturn = await _dbContext.MallReturns
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrderId == order.Id, cancellationToken);

        if (existingReturn != null && existingReturn.Status != MallReturnStatus.Rejected)
        {
            throw new BusinessException(400, "该订单已存在退货申请");
        }

        var payment = await _dbContext.MallPayments
            .AsNoTracking()
            // 已退款订单的支付单已转为 Refunded，仍需取到原支付单用于退货单关联
            .Where(p => p.OrderId == order.Id &&
                        (p.Status == MallPaymentStatus.Success ||
                         p.Status == MallPaymentStatus.Refunded))
            .OrderByDescending(p => p.PaidAt)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => new { p.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (payment is null)
        {
            throw new BusinessException(400, "未找到成功支付单，无法申请退货");
        }

        if (order.PaidAmount <= 0)
        {
            throw new BusinessException(400, "订单实付金额异常，无法申请退货");
        }

        var reason = MallAfterSaleRules.RequireReason(command.Reason);
        var description = MallAfterSaleRules.NormalizeDescription(command.Description);
        var evidenceImages = MallAfterSaleRules.NormalizeEvidence(command.EvidenceImages);

        order.BeginReturn();

        // 退货同样是整单退：退款金额按订单实付金额（含运费）计算，不支持部分退款
        var returnEntity = MallReturn.Create(
            MallNumberGenerator.Generate("RET"),
            order.Id,
            payment.Id,
            order.PaidAmount,
            reason,
            description,
            evidenceImages);

        _dbContext.MallReturns.Add(returnEntity);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MallReturnMapper.ToDto(returnEntity);
    }
}