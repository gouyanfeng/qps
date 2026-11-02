using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallReturns;

public class ShipMallReturnCommand : IRequest<MallReturnDto>
{
    public Guid? ReturnId { get; set; }

    public string? ReturnNo { get; set; }

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
        var returnEntity = await ResolveReturnAsync(command, cancellationToken);

        if (string.IsNullOrWhiteSpace(command.ExpressCompany) || string.IsNullOrWhiteSpace(command.TrackingNo))
            throw new BusinessException(400, "请填写快递公司和运单号");

        returnEntity.SubmitTracking(command.ExpressCompany.Trim(), command.TrackingNo.Trim());

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(returnEntity);
    }

    private async Task<MallReturn> ResolveReturnAsync(
        ShipMallReturnCommand command,
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
            r = await _dbContext.MallReturns
                .FirstOrDefaultAsync(x => x.ReturnNo == command.ReturnNo.Trim() && !x.IsDeleted, cancellationToken);
        }
        else
        {
            throw new BusinessException(400, "退货单标识不能为空");
        }

        if (r is null) throw new BusinessException(404, "退货单不存在");
        return r;
    }

    private static MallReturnDto ToDto(MallReturn r) => new()
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