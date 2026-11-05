using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Shared.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmVendors;

public class DeleteCrmVendorDemandCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}

public class DeleteCrmVendorDemandHandler : IRequestHandler<DeleteCrmVendorDemandCommand, bool>
{
    private readonly ICrmDbContext _dbContext;

    public DeleteCrmVendorDemandHandler(ICrmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(DeleteCrmVendorDemandCommand request, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.CrmVendorDemands
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
        if (plan == null)
        {
            throw new BusinessException(404, "采购需求不存在");
        }
        var vendor = await _dbContext.CrmVendors.FirstOrDefaultAsync(item => item.Id == plan.VendorId, cancellationToken)
            ?? throw new BusinessException(404, "厂商不存在");

        await CrmVendorDemands.EnsureNoUnclosedMatchOpportunitiesAsync(_dbContext, plan.Id, cancellationToken);
        plan.SoftDelete();
        await CrmVendorDemands.RefreshLatestAsync(_dbContext, vendor, cancellationToken, excludedPlanId: plan.Id);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
