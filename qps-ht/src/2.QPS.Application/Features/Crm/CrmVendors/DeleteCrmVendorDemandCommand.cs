using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Interfaces;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmVendors;

public class DeleteCrmVendorDemandCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class DeleteCrmVendorDemandHandler : IRequestHandler<DeleteCrmVendorDemandCommand, bool>
{
    private readonly IDbContext _dbContext;

    public DeleteCrmVendorDemandHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteCrmVendorDemandCommand request, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.CrmVendorDemands
            .FirstOrDefaultAsync(item => item.Id == request.Id && !item.IsDeleted, cancellationToken);
        if (plan == null)
        {
            throw new BusinessException(404, "采购需求不存在");
        }
        if (plan.Status != QPS.Domain.Entities.Crm.CrmVendorDemand.Pending)
            throw new BusinessException(400, "仅待确认采购需求可删除");
        var vendor = await _dbContext.CrmVendors.FirstOrDefaultAsync(item => item.Id == plan.VendorId, cancellationToken)
            ?? throw new BusinessException(404, "厂商不存在");

        plan.IsDeleted = true;
        await CrmVendorDemands.RefreshLatestAsync(_dbContext, vendor, cancellationToken, excludedPlanId: plan.Id);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
