using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Interfaces;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmVendors;

public class DeleteCrmPurchaseDemandCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class DeleteCrmPurchaseDemandHandler : IRequestHandler<DeleteCrmPurchaseDemandCommand, bool>
{
    private readonly IDbContext _dbContext;

    public DeleteCrmPurchaseDemandHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteCrmPurchaseDemandCommand request, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.CrmPurchaseDemands
            .FirstOrDefaultAsync(item => item.Id == request.Id && !item.IsDeleted, cancellationToken);
        if (plan == null)
        {
            throw new BusinessException(404, "采购需求不存在");
        }
        if (plan.Status != QPS.Domain.Entities.Crm.CrmPurchaseDemand.Pending)
            throw new BusinessException(400, "仅待确认采购需求可删除");
        var vendor = await _dbContext.CrmVendors.FirstOrDefaultAsync(item => item.Id == plan.VendorId, cancellationToken)
            ?? throw new BusinessException(404, "厂商不存在");

        plan.IsDeleted = true;
        await CrmPurchaseDemandProducts.ReplaceAsync(
            _dbContext,
            plan.Id,
            Array.Empty<string>(),
            cancellationToken);
        await CrmPurchaseDemands.RefreshLatestAsync(_dbContext, vendor, cancellationToken, excludedPlanId: plan.Id);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
