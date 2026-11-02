using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>
/// 录入运单并发货，仅已支付订单可发货。
/// </summary>
public class CreateMallShipmentCommand : IRequest<bool>
{
    /// <summary>管理端订单主键。</summary>
    public Guid? OrderId { get; set; }

    /// <summary>外部订单号。</summary>
    public string? OrderNo { get; set; }

    public CreateMallShipmentRequest Request { get; set; } = null!;
}

public class CreateMallShipmentHandler : IRequestHandler<CreateMallShipmentCommand, bool>
{
    private const int ExpressCompanyMaxLength = 50;
    private const int TrackingNoMaxLength = 64;
    private const int RemarkMaxLength = 200;

    private readonly IMallDbContext _dbContext;

    public CreateMallShipmentHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(CreateMallShipmentCommand command, CancellationToken cancellationToken)
    {
        var order = await MallOrderResolver.ResolveAsync(
            _dbContext,
            command.OrderId,
            command.OrderNo,
            cancellationToken);

        var expressCompany = NormalizeRequired(command.Request.ExpressCompany, ExpressCompanyMaxLength, "快递公司");
        var trackingNo = NormalizeRequired(command.Request.TrackingNo, TrackingNoMaxLength, "快递单号");
        var remark = command.Request.Remark?.Trim() ?? string.Empty;

        if (remark.Length > RemarkMaxLength)
        {
            throw new BusinessException(400, $"运单备注不能超过 {RemarkMaxLength} 个字符");
        }

        var trackingExists = await _dbContext.MallShipments
            .AnyAsync(shipment => shipment.TrackingNo == trackingNo && !shipment.IsDeleted, cancellationToken);

        if (trackingExists)
        {
            throw new BusinessException(400, "快递单号已存在");
        }

        var shipmentExists = await _dbContext.MallShipments
            .AnyAsync(shipment => shipment.OrderId == order.Id && !shipment.IsDeleted, cancellationToken);

        if (shipmentExists)
        {
            throw new BusinessException(400, "该订单已存在运单");
        }

        order.MarkShipped();

        _dbContext.MallShipments.Add(MallShipment.Create(
            MallNumberGenerator.Generate("SHP"),
            order.Id,
            expressCompany,
            trackingNo,
            remark));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static string NormalizeRequired(string? value, int maxLength, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessException(400, $"请输入{fieldName}");
        }

        if (normalized.Length > maxLength)
        {
            throw new BusinessException(400, $"{fieldName}不能超过 {maxLength} 个字符");
        }

        return normalized;
    }
}
