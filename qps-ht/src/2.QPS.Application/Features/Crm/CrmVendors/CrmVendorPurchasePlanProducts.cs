using Microsoft.EntityFrameworkCore;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;

namespace QPS.Application.Features.Crm.CrmVendors;

public static class CrmVendorPurchasePlanProducts
{
    public static async Task ReplaceAsync(
        IDbContext dbContext,
        Guid planId,
        IEnumerable<string> productNames,
        CancellationToken cancellationToken)
    {
        var oldAttributes = await dbContext.CrmBusinessEntityAttributes
            .Where(attribute =>
                !attribute.IsDeleted &&
                attribute.EntityType == CrmCodes.VendorPurchasePlanEntityType &&
                attribute.EntityId == planId &&
                attribute.AttributeCode == CrmCodes.PurchaseProductAttributeCode)
            .ToListAsync(cancellationToken);

        foreach (var attribute in oldAttributes)
        {
            attribute.IsDeleted = true;
        }

        // The filtered unique index requires old rows to be soft-deleted before re-adding a name.
        if (oldAttributes.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var sortOrder = 1;
        foreach (var productName in Normalize(productNames))
        {
            dbContext.CrmBusinessEntityAttributes.Add(new CrmBusinessEntityAttribute(
                CrmCodes.VendorPurchasePlanEntityType,
                planId,
                CrmCodes.PurchaseProductAttributeCode,
                productName,
                sortOrder++));
        }
    }

    private static List<string> Normalize(IEnumerable<string> productNames)
    {
        return productNames
            .Select(productName => productName.Trim())
            .Where(productName => !string.IsNullOrWhiteSpace(productName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
