using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Application.Features.MallOrders;
using QPS.Mall.Contracts;
using QPS.Mall.Contracts.DTOs;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallReturns;

public class ShipMallReturnCommand : IRequest<MallReturnDto>
{
    public Guid? ReturnId { get; set; }

    public string? ReturnNo { get; set; }

    /// <summary>移动端传当前会员；管理端代客操作时传 null，跳过归属校验。</summary>
    public Guid? MemberId { get; set; }

    public string ExpressCompany { get; set; } = string.Empty;

    public string TrackingNo { get; set; } = string.Empty;
}

public class ShipMallReturnHandler : IRequestHandler<ShipMallReturnCommand, MallReturnDto>
{
    private readonly IMallDbContext _dbContext;

    public ShipMallReturnHandler(IMallDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MallReturnDto> Handle(ShipMallReturnCommand command, CancellationToken cancellationToken)
    {
        var returnEntity = await MallReturnMapper.ResolveAsync(
            _dbContext,
            command.ReturnId,
            command.ReturnNo,
            cancellationToken);

        var order = await _dbContext.MallOrders
            .FirstOrDefaultAsync(x => x.Id == returnEntity.OrderId, cancellationToken);

        if (order is null) throw new BusinessException(404, "退货单关联订单不存在");

        MallOrderResolver.EnsureOwnership(order, command.MemberId);

        if (string.IsNullOrWhiteSpace(command.ExpressCompany) || string.IsNullOrWhiteSpace(command.TrackingNo))
            throw new BusinessException(400, "请填写快递公司和运单号");

        returnEntity.SubmitTracking(command.ExpressCompany.Trim(), command.TrackingNo.Trim());

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MallReturnMapper.ToDto(returnEntity);
    }
}