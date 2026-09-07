using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm.CrmVendors;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm.CrmVendors;

internal static class CrmVendorDemands
{
    public static async Task EnsureNoUnclosedMatchOpportunitiesAsync(
        IDbContext dbContext,
        Guid demandId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.CrmMatchOpportunities.AnyAsync(
            opportunity => opportunity.DemandId == demandId &&
                (opportunity.Status == CrmMatchOpportunity.InProgress ||
                 opportunity.Status == CrmMatchOpportunity.Paused),
            cancellationToken);
        if (exists)
        {
            throw new BusinessException(400, "存在未关闭的撮合机会，请先关闭撮合机会");
        }
    }

    public static async Task EnsureItemsNotReferencedByMatchOpportunityAsync(
        IDbContext dbContext,
        IReadOnlyCollection<Guid> demandItemIds,
        CancellationToken cancellationToken)
    {
        if (demandItemIds.Count == 0)
        {
            return;
        }

        var exists = await dbContext.CrmMatchOpportunities.AnyAsync(
            opportunity => demandItemIds.Contains(opportunity.DemandItemId),
            cancellationToken);
        if (exists)
        {
            throw new BusinessException(400, "采购明细已关联撮合机会，不能删除");
        }
    }

    public static async Task EnsureUnclosedMatchOpportunitiesNotAffectedByUpdateAsync(
        IDbContext dbContext,
        CrmVendorDemand demand,
        CrmVendorDemandSaveRequest request,
        CancellationToken cancellationToken)
    {
        var protectedItemIds = await dbContext.CrmMatchOpportunities
            .Where(opportunity => opportunity.DemandId == demand.Id &&
                (opportunity.Status == CrmMatchOpportunity.InProgress || opportunity.Status == CrmMatchOpportunity.Paused))
            .Select(opportunity => opportunity.DemandItemId)
            .ToListAsync(cancellationToken);
        if (protectedItemIds.Count == 0)
        {
            return;
        }

        if (!string.Equals(demand.ReceivingAddress.Trim(), request.ReceivingAddress.Trim(), StringComparison.Ordinal))
        {
            throw new BusinessException(400, "存在未关闭的撮合机会，不能修改需求收货地");
        }

        var requestItems = request.Items
            .Where(item => item.Id.HasValue)
            .ToDictionary(item => item.Id!.Value);
        foreach (var itemId in protectedItemIds)
        {
            var currentItem = demand.Items.FirstOrDefault(item => item.Id == itemId);
            if (currentItem == null || !requestItems.TryGetValue(itemId, out var requestItem))
            {
                throw new BusinessException(400, "存在未关闭的撮合机会，不能删除关联采购明细");
            }

            if (HasChanged(currentItem, requestItem))
            {
                throw new BusinessException(400, "存在未关闭的撮合机会，不能修改关联采购明细的撮合关键字段");
            }
        }
    }

    public static async Task RefreshLatestAsync(
        IDbContext dbContext,
        CrmVendor vendor,
        CancellationToken cancellationToken,
        CrmVendorDemand? candidatePlan = null,
        Guid? excludedPlanId = null)
    {
        var latestPlan = await dbContext.CrmVendorDemands
            .Where(plan =>
                !plan.IsDeleted &&
                plan.VendorId == vendor.Id &&
                (!excludedPlanId.HasValue || plan.Id != excludedPlanId.Value))
            .OrderByDescending(plan => plan.DemandAt)
            .ThenByDescending(plan => plan.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (candidatePlan is { IsDeleted: false } &&
            candidatePlan.VendorId == vendor.Id &&
            (!excludedPlanId.HasValue || candidatePlan.Id != excludedPlanId.Value) &&
            IsLater(candidatePlan, latestPlan))
        {
            latestPlan = candidatePlan;
        }

        vendor.UpdateLatestPurchaseDemandSummary(
            latestPlan?.DemandAt,
            latestPlan?.DemandName ?? string.Empty);
    }

    private static bool IsLater(CrmVendorDemand plan, CrmVendorDemand? other)
    {
        if (other == null)
        {
            return true;
        }

        var planTime = plan.DemandAt;
        var otherTime = other.DemandAt;
        return planTime > otherTime || (planTime == otherTime && plan.CreatedAt > other.CreatedAt);
    }

    private static bool HasChanged(CrmVendorDemandItem item, CrmVendorDemandItemRequest request) =>
        !string.Equals(item.ProductName.Trim(), request.ProductName.Trim(), StringComparison.Ordinal) ||
        item.Quantity != request.Quantity ||
        !string.Equals(item.QuantityUnit.Trim(), request.QuantityUnit.Trim(), StringComparison.Ordinal) ||
        !string.Equals(item.Specification.Trim(), request.Specification.Trim(), StringComparison.Ordinal) ||
        !string.Equals(item.QualityRequirement.Trim(), request.QualityRequirement.Trim(), StringComparison.Ordinal) ||
        item.TargetPrice != request.TargetPrice ||
        !string.Equals(item.PriceUnit.Trim(), request.PriceUnit.Trim(), StringComparison.Ordinal) ||
        item.ExpectedDeliveryAt != request.ExpectedDeliveryAt;
}
