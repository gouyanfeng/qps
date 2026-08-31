using Microsoft.EntityFrameworkCore;
using QPS.Application.Interfaces;
using QPS.Domain.Entities.Crm;

namespace QPS.Application.Features.Crm.CrmVendors;

public static class CrmVendorPurchasePlanProducts
{
    public static async Task ReplaceAsync(
        IDbContext dbContext,
        Guid planId,
        IEnumerable<string>? productNames,
        CancellationToken cancellationToken)
    {
        var productNamesToKeep = Normalize(productNames);
        var attributes = await dbContext.CrmBusinessEntityAttributes
            .IgnoreQueryFilters()
            .Where(attribute =>
                attribute.EntityType == CrmCodes.VendorPurchasePlanEntityType &&
                attribute.EntityId == planId &&
                attribute.AttributeCode == CrmCodes.PurchaseProductAttributeCode)
            .ToListAsync(cancellationToken);

        var retainedAttributeIds = new HashSet<Guid>();
        var sortOrder = 1;
        foreach (var productName in productNamesToKeep)
        {
            var attribute = attributes
                .Where(item => string.Equals(
                    item.AttributeValue.Trim(),
                    productName,
                    StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.IsDeleted)
                .ThenBy(item => item.CreatedAt)
                .FirstOrDefault();

            if (attribute == null)
            {
                attribute = new CrmBusinessEntityAttribute(
                    CrmCodes.VendorPurchasePlanEntityType,
                    planId,
                    CrmCodes.PurchaseProductAttributeCode,
                    productName,
                    sortOrder);
                attributes.Add(attribute);
                dbContext.CrmBusinessEntityAttributes.Add(attribute);
            }
            else
            {
                attribute.IsDeleted = false;
                attribute.Update(
                    CrmCodes.VendorPurchasePlanEntityType,
                    planId,
                    CrmCodes.PurchaseProductAttributeCode,
                    productName,
                    sortOrder,
                    attribute.Remark);
            }

            retainedAttributeIds.Add(attribute.Id);
            sortOrder++;
        }

        foreach (var attribute in attributes.Where(attribute =>
                     !attribute.IsDeleted && !retainedAttributeIds.Contains(attribute.Id)))
        {
            attribute.IsDeleted = true;
        }
    }

    private static List<string> Normalize(IEnumerable<string>? productNames)
    {
        return (productNames ?? [])
            .Where(productName => !string.IsNullOrWhiteSpace(productName))
            .Select(productName => productName!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
