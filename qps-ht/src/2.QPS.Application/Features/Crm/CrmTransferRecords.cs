using Microsoft.EntityFrameworkCore;
using QPS.Application.Contracts.Crm;
using QPS.Application.Interfaces;

namespace QPS.Application.Features.Crm;

public static class CrmTransferRecords
{
    public static async Task<List<CrmTransferRecordDto>> GetAsync(
        IDbContext dbContext,
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var records = await dbContext.CrmTransferRecords
            .AsNoTracking()
            .Where(record =>
                record.EntityType == entityType &&
                record.EntityId == entityId)
            .OrderByDescending(record => record.CreatedAt)
            .Select(record => new CrmTransferRecordDto
            {
                Id = record.Id,
                ActionType = record.ActionType,
                EntityType = record.EntityType,
                EntityId = record.EntityId,
                FromOwnerUserId = record.FromOwnerUserId,
                ToOwnerUserId = record.ToOwnerUserId,
                OperatorUserId = record.OperatorUserId,
                Remark = record.Remark,
                CreatedAt = record.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var userIds = records
            .SelectMany(record => new[] { record.FromOwnerUserId, record.ToOwnerUserId, record.OperatorUserId })
            .Where(userId => userId.HasValue)
            .Select(userId => userId!.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return records;
        }

        var userNames = await dbContext.SystemUsers
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(
                user => user.Id,
                user => string.IsNullOrWhiteSpace(user.RealName) ? user.Username : user.RealName,
                cancellationToken);

        foreach (var record in records)
        {
            record.FromOwnerUserName = GetUserName(userNames, record.FromOwnerUserId);
            record.ToOwnerUserName = GetUserName(userNames, record.ToOwnerUserId);
            record.OperatorUserName = GetUserName(userNames, record.OperatorUserId);
        }

        return records;
    }

    private static string GetUserName(Dictionary<Guid, string> userNames, Guid? userId)
    {
        return userId.HasValue && userNames.TryGetValue(userId.Value, out var userName)
            ? userName
            : string.Empty;
    }
}
