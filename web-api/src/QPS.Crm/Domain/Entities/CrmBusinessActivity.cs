using QPS.Shared.Common;

namespace QPS.Crm.Domain.Entities;

public class CrmBusinessActivity : BaseEntity
{
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string ActionType { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;

    private CrmBusinessActivity()
    {
    }

    public static CrmBusinessActivity Create(string entityType, Guid entityId, string actionType, string content)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new InvalidOperationException("业务活动对象类型不能为空");
        }

        if (entityId == Guid.Empty)
        {
            throw new InvalidOperationException("业务活动对象不能为空");
        }

        if (string.IsNullOrWhiteSpace(actionType))
        {
            throw new InvalidOperationException("业务活动类型不能为空");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("业务活动内容不能为空");
        }

        return new CrmBusinessActivity
        {
            EntityType = Trim(entityType),
            EntityId = entityId,
            ActionType = Trim(actionType),
            Content = Trim(content)
        };
    }

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;
}
