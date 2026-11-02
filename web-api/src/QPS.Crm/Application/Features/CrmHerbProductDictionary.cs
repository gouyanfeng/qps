using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Crm.Application.Features;

public static class CrmHerbProductDictionary
{
    public static async Task ValidateActiveNamesAsync(
        ICrmDbContext dbContext,
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
        var entries = await dbContext.SystemDataDictionaries
            .AsNoTracking()
            .Where(entry => entry.ParentId == rootId)
            .Select(entry => new { entry.Name, entry.IsActive })
            .ToListAsync(cancellationToken);

        var requested = requestedNames.ToHashSet(StringComparer.Ordinal);
        var matchedEntries = entries.Where(entry => requested.Contains(entry.Name)).ToList();

        var activeNames = matchedEntries
            .Where(entry => entry.IsActive)
            .Select(entry => entry.Name)
            .ToHashSet(StringComparer.Ordinal);

        var inactiveOrMissingNames = requestedNames
            .Where(name => !activeNames.Contains(name))
            .ToList();

        if (inactiveOrMissingNames.Count == 0)
        {
            return;
        }

        var existingNames = matchedEntries
            .Select(entry => entry.Name)
            .ToHashSet(StringComparer.Ordinal);

        if (inactiveOrMissingNames.Any(name => !existingNames.Contains(name)))
        {
            throw new BusinessException(400, "品类未在中药材品类字典中维护。");
        }

        throw new BusinessException(400, "品类已停用，请重新选择。");
    }

    private static async Task<Guid> GetRootIdAsync(
        ICrmDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var rootId = await dbContext.SystemDataDictionaries
            .AsNoTracking()
            .Where(entry => entry.Code == CrmCodes.HerbProductDictionaryCode)
            .Select(entry => (Guid?)entry.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return rootId ?? throw new BusinessException(500, "中药材品类字典未初始化。");
    }
}
