using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Interfaces;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmVendors;

public class ChangeCrmPurchaseDemandStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public CrmPurchaseDemandStatusRequest Request { get; set; } = null!;
}
public class ChangeCrmPurchaseDemandStatusHandler : IRequestHandler<ChangeCrmPurchaseDemandStatusCommand, bool>
{
    private readonly IDbContext _dbContext;
    public ChangeCrmPurchaseDemandStatusHandler(IDbContext dbContext) => _dbContext = dbContext;
    public async Task<bool> Handle(ChangeCrmPurchaseDemandStatusCommand request, CancellationToken cancellationToken)
    {
        var demand = await _dbContext.CrmPurchaseDemands.Include(item => item.Items).FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken) ?? throw new BusinessException(404, "采购需求不存在");
        try { demand.ChangeStatus(request.Request.Status.Trim(), request.Request.ClosedReason); }
        catch (InvalidOperationException exception) { throw new BusinessException(400, exception.Message); }
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
