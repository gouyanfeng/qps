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
        var activeNames = (await dbContext.SystemDataDictionaries
            .Where(item => !item.IsDeleted && item.ParentId == rootId && item.IsActive && requestedNames.Contains(item.Name))
            .Select(item => item.Name)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        var inactiveOrMissingNames = requestedNames
            .Where(name => !activeNames.Contains(name))
            .ToList();

        if (inactiveOrMissingNames.Count == 0)
        {
            return;
        }

        var existingNames = (await dbContext.SystemDataDictionaries
            .Where(item => !item.IsDeleted && item.ParentId == rootId && inactiveOrMissingNames.Contains(item.Name))
            .Select(item => item.Name)
            .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);

        if (inactiveOrMissingNames.Any(name => !existingNames.Contains(name)))
        {
            throw new BusinessException(400, "品类未在中药材品类字典中维护。");
        }

        throw new BusinessException(400, "品类已停用，请重新选择。");
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
