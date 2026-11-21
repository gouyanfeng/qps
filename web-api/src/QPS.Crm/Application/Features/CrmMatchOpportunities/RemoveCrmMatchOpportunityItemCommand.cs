using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts;
using QPS.Crm.Contracts.Permissions;
using QPS.Crm.Domain.Entities;
using QPS.Shared.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features.CrmMatchOpportunities;

[Permission(CrmPermissionCodes.CRM_MATCH_OPPORTUNITY_MANAGE)]
public class RemoveCrmMatchOpportunityItemCommand : IRequest<bool>
{
    public Guid OpportunityId { get; set; }
    public Guid ItemId { get; set; }
}

public class RemoveCrmMatchOpportunityItemHandler(ICrmDbContext dbContext)
    : IRequestHandler<RemoveCrmMatchOpportunityItemCommand, bool>
{
    public async Task<bool> Handle(RemoveCrmMatchOpportunityItemCommand request, CancellationToken cancellationToken)
    {
        var opportunity = await CrmMatchOpportunityCommandGuards.FindOpportunityAsync(dbContext, request.OpportunityId, cancellationToken);
        CrmMatchOpportunityCommandGuards.EnsureInProgress(opportunity);

        var item = await dbContext.CrmMatchOpportunityItems.FirstOrDefaultAsync(
                x => x.Id == request.ItemId && x.OpportunityId == opportunity.Id, cancellationToken)
            ?? throw new BusinessException(404, "撮合明细不存在");

        if (item.Role == CrmMatchOpportunityItem.Initiator)
        {
            throw new BusinessException(400, "发起方不能移除");
        }

        var label = await DescribeItemAsync(dbContext, item, cancellationToken);
        dbContext.CrmMatchOpportunityItems.Remove(item);
        dbContext.CrmBusinessActivities.Add(CrmMatchOpportunitySource.CreateActivity(
            opportunity.Id, "移除候选", $"移除候选：{label}"));
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<string> DescribeItemAsync(ICrmDbContext dbContext, CrmMatchOpportunityItem item, CancellationToken cancellationToken)
    {
        if (item.EntityType == CrmMatchOpportunityItem.EntityTypeSupply)
        {
            var supply = await dbContext.CrmHerbBaseSupplies
                .Include(x => x.HerbBase)
                .FirstOrDefaultAsync(x => x.Id == item.EntityId, cancellationToken);
            return supply == null ? "已失效供应" : $"{supply.HerbBase?.BaseName ?? string.Empty}{supply.ProductName}";
        }
        else
        {
            var demandItem = await dbContext.CrmVendorDemandItems
                .Include(x => x.VendorDemand)
                .FirstOrDefaultAsync(x => x.Id == item.EntityId, cancellationToken);
            if (demandItem?.VendorDemand == null) return "已失效采购明细";
            var vendor = await dbContext.CrmVendors.FirstOrDefaultAsync(x => x.Id == demandItem.VendorDemand.VendorId, cancellationToken);
            return $"{vendor?.VendorName ?? string.Empty}{demandItem.ProductName}";
        }
    }
}
