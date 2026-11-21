using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts;
using QPS.Crm.Domain.Entities;

namespace QPS.Crm.Application.Features.CrmHerbBases;

/// <summary>
/// 基地品类（CrmBusinessEntityAttributes / CRM_HERB_BASE_PRODUCT）写入。
/// 列表与详情的品类列只读这张表，因此基地保存时全量覆盖，供应保存时单向追加。
/// </summary>
internal static class CrmHerbBaseProductSync
{
    public static async Task ReplaceAsync(
        ICrmDbContext dbContext,
        Guid herbBaseId,
        IEnumerable<string>? productNames,
        CancellationToken cancellationToken)
    {
        var values = Normalize(productNames);
        var existing = await dbContext.CrmBusinessEntityAttributes
            .Where(attribute =>
                attribute.EntityType == CrmCodes.HerbBaseEntityType &&
                attribute.EntityId == herbBaseId &&
                attribute.AttributeCode == CrmCodes.AttributeCodes.HerbBaseProduct)
            .ToListAsync(cancellationToken);

        dbContext.CrmBusinessEntityAttributes.RemoveRange(existing);
        AddNew(dbContext, herbBaseId, values, 1);
    }

    /// <summary>
    /// 供应保存时把品类单向同步到基地属性，已存在（忽略大小写）则跳过。
    /// </summary>
    public static async Task AppendAsync(
        ICrmDbContext dbContext,
        Guid herbBaseId,
        string? productName,
        CancellationToken cancellationToken)
    {
        var value = productName?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var existing = await dbContext.CrmBusinessEntityAttributes
            .Where(attribute =>
                attribute.EntityType == CrmCodes.HerbBaseEntityType &&
                attribute.EntityId == herbBaseId &&
                attribute.AttributeCode == CrmCodes.AttributeCodes.HerbBaseProduct)
            .Select(attribute => attribute.AttributeValue)
            .ToListAsync(cancellationToken);

        if (existing.Any(item => string.Equals(item.Trim(), value, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        AddNew(dbContext, herbBaseId, [value], existing.Count + 1);
    }

    private static List<string> Normalize(IEnumerable<string>? productNames) =>
        (productNames ?? [])
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static void AddNew(ICrmDbContext dbContext, Guid herbBaseId, List<string> values, int startSortOrder)
    {
        var sortOrder = startSortOrder;
        foreach (var value in values)
        {
            dbContext.CrmBusinessEntityAttributes.Add(new CrmBusinessEntityAttribute(
                CrmCodes.HerbBaseEntityType,
                herbBaseId,
                CrmCodes.AttributeCodes.HerbBaseProduct,
                value,
                sortOrder++));
        }
    }
}
