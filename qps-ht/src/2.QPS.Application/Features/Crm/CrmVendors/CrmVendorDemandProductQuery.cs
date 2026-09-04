using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Interfaces;

namespace QPS.Application.Features.Crm.CrmVendors;

public static class CrmVendorDemandProductQuery
{
    public static IQueryable<CrmVendorDemandProductItem> GetEffectiveItems(IDbContext dbContext)
    {
        return GetEffectiveItems(dbContext, []);
    }

    public static IQueryable<CrmVendorDemandProductItem> GetEffectiveItems(
        IDbContext dbContext,
        IReadOnlyCollection<Guid> vendorIds)
    {
        var demands = dbContext.CrmVendorDemands
            .Where(demand => !demand.IsDeleted);
        if (vendorIds.Count > 0)
        {
            demands = demands.Where(demand => vendorIds.Contains(demand.VendorId));
        }

        return from demand in demands
               join item in dbContext.CrmVendorDemandItems on demand.Id equals item.VendorDemandId
               where !item.IsDeleted
               select new CrmVendorDemandProductItem(
                   demand.VendorId,
                   item.Id,
                   item.ProductName,
                   item.SortOrder,
                   item.Remark);
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
                        Id = item.ItemId,
                        ProductName = item.ProductName,
                        SortOrder = item.SortOrder,
                        Remark = item.Remark
                    })
                    .ToList());
    }
}

public sealed record CrmVendorDemandProductItem(
    Guid VendorId,
    Guid ItemId,
    string ProductName,
    int SortOrder,
    string Remark);
