using QPS.Crm.Contracts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts.DTOs;
using QPS.Crm.Contracts.DTOs.CrmVendors;
using QPS.Shared.Contracts;
using QPS.Crm.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmVendors;

public class ChangeCrmVendorDemandStatusCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public CrmVendorDemandStatusRequest Request { get; set; } = null!;
}
public class ChangeCrmVendorDemandStatusHandler : IRequestHandler<ChangeCrmVendorDemandStatusCommand, bool>
{
    private readonly ICrmDbContext _dbContext;
    public ChangeCrmVendorDemandStatusHandler(ICrmDbContext dbContext) => _dbContext = dbContext;
    public async Task<bool> Handle(ChangeCrmVendorDemandStatusCommand request, CancellationToken cancellationToken)
    {
        var demand = await _dbContext.CrmVendorDemands.Include(item => item.Items).FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken) ?? throw new BusinessException(404, "采购需求不存在");
        var targetStatus = request.Request.Status.Trim();
        if (targetStatus is CrmVendorDemand.Completed or CrmVendorDemand.Closed)
        {
            await CrmVendorDemands.EnsureNoUnclosedMatchOpportunitiesAsync(_dbContext, demand.Id, cancellationToken);
        }

        demand.ChangeStatus(targetStatus, request.Request.ClosedReason);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
