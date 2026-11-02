using QPS.Crm.Contracts;
using QPS.Crm.Contracts.DTOs.CrmVendors;

namespace QPS.Crm.Application.Features.CrmVendors;

public static class CrmVendorOwners
{
    public static async Task FillAsync(ICrmDbContext dbContext, List<CrmVendorDto> vendors, CancellationToken cancellationToken)
    {
        var users = await CrmUserNames.GetAsync(
            dbContext,
            vendors.Where(vendor => vendor.OwnerUserId.HasValue).Select(vendor => vendor.OwnerUserId!.Value),
            cancellationToken);

        foreach (var vendor in vendors)
        {
            if (vendor.OwnerUserId.HasValue &&
                users.TryGetValue(vendor.OwnerUserId.Value, out var owner))
            {
                vendor.OwnerUserName = owner.RealName;
            }
        }
    }
}
