using QPS.Domain.Common;

namespace QPS.Domain.Entities.Crm;

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
