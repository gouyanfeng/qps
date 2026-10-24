using Microsoft.EntityFrameworkCore;
using QPS.Crm.Contracts;

namespace QPS.Crm.Application.Features;

/// <summary>
/// CRM 展示层需要的用户姓名查询：按 Id 批量读取用户名与姓名。
/// </summary>
public static class CrmUserNames
{
    public static async Task<Dictionary<Guid, (string Username, string RealName)>> GetAsync(
        ICrmDbContext dbContext,
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var users = await dbContext.SystemUsers
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .Select(user => new { user.Id, user.Username, user.RealName })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(
            user => user.Id,
            user => (user.Username, user.RealName));
    }

    /// <summary>
    /// 账号姓名：优先使用姓名，为空时回退到用户名。
    /// </summary>
    public static string DisplayName(string username, string realName) =>
        string.IsNullOrWhiteSpace(realName) ? username : realName;
}
