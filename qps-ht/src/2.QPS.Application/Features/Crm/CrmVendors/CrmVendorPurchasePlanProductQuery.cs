using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Interfaces;

namespace QPS.Application.Features.Crm.CrmVendors;

public static class CrmVendorPurchasePlanProductQuery
{
    public static IQueryable<CrmVendorPurchasePlanProductItem> GetEffectiveItems(IDbContext dbContext)
    {
        return GetEffectiveItems(dbContext, []);
    }

    public static IQueryable<CrmVendorPurchasePlanProductItem> GetEffectiveItems(
        IDbContext dbContext,
        IReadOnlyCollection<Guid> vendorIds)
    {
        var plans = dbContext.CrmVendorPurchasePlans
            .Where(plan => !plan.IsDeleted);
        if (vendorIds.Count > 0)
        {
            plans = plans.Where(plan => vendorIds.Contains(plan.VendorId));
        }

        return from plan in plans
               join attribute in dbContext.CrmBusinessEntityAttributes on plan.Id equals attribute.EntityId
               where !attribute.IsDeleted &&
                     attribute.EntityType == CrmCodes.VendorPurchasePlanEntityType &&
                     attribute.AttributeCode == CrmCodes.PurchaseProductAttributeCode
               select new CrmVendorPurchasePlanProductItem(
                   plan.VendorId,
                   attribute.Id,
                   attribute.AttributeValue,
                   attribute.SortOrder,
                   attribute.Remark);
    }

    public static IQueryable<Guid> GetVendorIdsWithProducts(IDbContext dbContext)
    {
        return GetEffectiveItems(dbContext)
            .Select(item => item.VendorId)
            .Distinct();
    }

    public static async Task<Dictionary<Guid, List<string>>> GetNamesAsync(
        IDbContext dbContext,
        IEnumerable<Guid> vendorIds,
        CancellationToken cancellationToken)
    {
        var products = await GetProductsAsync(dbContext, vendorIds, cancellationToken);
        return products.ToDictionary(
            item => item.Key,
            item => item.Value.Select(product => product.ProductName).ToList());
    }

    public static async Task<Dictionary<Guid, List<CrmVendorProductDto>>> GetProductsAsync(
        IDbContext dbContext,
        IEnumerable<Guid> vendorIds,
        CancellationToken cancellationToken)
    {
        var ids = vendorIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var items = await GetEffectiveItems(dbContext, ids)
            .ToListAsync(cancellationToken);

        return items
            .GroupBy(item => item.VendorId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .GroupBy(item => item.ProductName, StringComparer.OrdinalIgnoreCase)
                    .Select(productGroup => productGroup
                        .OrderBy(item => item.SortOrder)
                        .ThenBy(item => item.ProductName)
                        .First())
                    .OrderBy(item => item.SortOrder)
                    .ThenBy(item => item.ProductName)
                    .Select(item => new CrmVendorProductDto
                    {
                        Id = item.AttributeId,
                        ProductName = item.ProductName,
                        SortOrder = item.SortOrder,
                        Remark = item.Remark
                    })
                    .ToList());
    }
}

public sealed record CrmVendorPurchasePlanProductItem(
    Guid VendorId,
    Guid AttributeId,
    string ProductName,
    int SortOrder,
    string Remark);
