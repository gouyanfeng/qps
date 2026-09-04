using Microsoft.EntityFrameworkCore;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;

namespace QPS.Application.Features.Crm.CrmVendors;

internal static class CrmVendorDemands
{
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
}
