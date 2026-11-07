using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallShipments;

/// <summary>
/// 作废运单：运单作废并释放运单号，订单回退到已支付（待发货），作废记录保留。
/// </summary>
public class VoidMallShipmentCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public class VoidMallShipmentHandler : IRequestHandler<VoidMallShipmentCommand, bool>
{
    private const int VoidReasonMaxLength = 200;

    private readonly IMallDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public VoidMallShipmentHandler(IMallDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(VoidMallShipmentCommand request, CancellationToken cancellationToken)
    {
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new BusinessException(400, "请填写作废原因");
        }

        if (reason.Length > VoidReasonMaxLength)
        {
            throw new BusinessException(400, $"作废原因不能超过 {VoidReasonMaxLength} 个字符");
        }

        var shipment = await _dbContext.MallShipments
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);

        if (shipment is null)
        {
            throw new BusinessException(404, "发货单不存在");
        }

        var order = await _dbContext.MallOrders
            .FirstOrDefaultAsync(item => item.Id == shipment.OrderId, cancellationToken);

        if (order is null)
        {
            throw new BusinessException(404, "发货单关联订单不存在");
        }

        // 先校验订单可回退，再落运单作废，避免订单状态不变却把运单作废
        if (!shipment.IsVoided)
        {
            order.RevertShipment();
            shipment.Void(reason, _currentUserService.Username ?? string.Empty);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
