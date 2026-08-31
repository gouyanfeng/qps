using Microsoft.EntityFrameworkCore;
using QPS.Application.Interfaces;
using QPS.Domain.Exceptions;

namespace QPS.Application.Features.Crm;

public static class CrmHerbProductDictionary
{
    public static async Task<HashSet<string>> GetActiveNamesAsync(
        IDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var rootId = await GetRootIdAsync(dbContext, cancellationToken);
        return (await dbContext.SystemDataDictionaries
                .Where(item => !item.IsDeleted && item.ParentId == rootId && item.IsActive)
                .Select(item => item.Name)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
    }

    public static async Task ValidateActiveNamesAsync(
        IDbContext dbContext,
        IEnumerable<string> names,
        CancellationToken cancellationToken)
    {
        var requestedNames = names
            .Select(name => name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (requestedNames.Count == 0)
        {
            return;
        }

        var rootId = await GetRootIdAsync(dbContext, cancellationToken);
        var dictionaryItems = await dbContext.SystemDataDictionaries
            .Where(item => !item.IsDeleted && item.ParentId == rootId && requestedNames.Contains(item.Name))
            .Select(item => new { item.Name, item.IsActive })
            .ToListAsync(cancellationToken);
        var existingNames = dictionaryItems.Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        if (requestedNames.Any(name => !existingNames.Contains(name)))
        {
            throw new BusinessException(400, "品类未在中药材品类字典中维护。");
        }

        if (dictionaryItems.Any(item => !item.IsActive))
        {
            throw new BusinessException(400, "品类已停用，请重新选择。");
        }
    }

    private static async Task<Guid> GetRootIdAsync(IDbContext dbContext, CancellationToken cancellationToken)
    {
        var rootId = await dbContext.SystemDataDictionaries
            .Where(item => !item.IsDeleted && item.Code == CrmCodes.HerbProductDictionaryCode)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return rootId ?? throw new BusinessException(500, "中药材品类字典未初始化。");
    }
}
