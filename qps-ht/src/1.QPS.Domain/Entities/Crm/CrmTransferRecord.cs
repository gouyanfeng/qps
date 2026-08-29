using QPS.Domain.Common;

using QPS.Domain.Exceptions;

namespace QPS.Domain.Entities.Crm;

public class CrmTransferRecord : BaseEntity
{
    public string ActionType { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public Guid? FromOwnerUserId { get; private set; }

    public Guid? ToOwnerUserId { get; private set; }

    public Guid? OperatorUserId { get; private set; }

    public string Remark { get; private set; } = string.Empty;

    private CrmTransferRecord()
    {
    }

    private CrmTransferRecord(
        string actionType,
        string entityType,
        Guid entityId,
        Guid? fromOwnerUserId,
        Guid? toOwnerUserId,
        Guid? operatorUserId,
        string remark)
    {
        ActionType = actionType;
        EntityType = entityType;
        EntityId = entityId;
        FromOwnerUserId = fromOwnerUserId;
        ToOwnerUserId = toOwnerUserId;
        OperatorUserId = operatorUserId;
        Remark = remark;
    }

    public static CrmTransferRecord Create(
        string entityType,
        Guid entityId,
        Guid? fromOwnerUserId,
        Guid? toOwnerUserId,
        Guid? operatorUserId,
        string remark)
    {
        return !fromOwnerUserId.HasValue && !toOwnerUserId.HasValue
            ? CreateEntry(entityType, entityId, null, operatorUserId, remark)
            : CreateOwnerChange(entityType, entityId, fromOwnerUserId, toOwnerUserId, operatorUserId, remark);
    }

    public static CrmTransferRecord CreateEntry(string entityType, Guid entityId, Guid? ownerUserId, Guid? operatorUserId, string remark)
    {
        EnsureEntity(entityType, entityId);
        return new CrmTransferRecord(CrmTransferActionType.Entry, entityType, entityId, null, ownerUserId, operatorUserId, remark);
    }

    public static CrmTransferRecord CreateOwnerChange(
        string entityType,
        Guid entityId,
        Guid? fromOwnerUserId,
        Guid? toOwnerUserId,
        Guid? operatorUserId,
        string remark)
    {
        EnsureEntity(entityType, entityId);
        if (fromOwnerUserId == toOwnerUserId)
            throw new BusinessException(400, fromOwnerUserId.HasValue ? "负责人未变化，无需流转" : "待分配对象不能退回待分配池");

        var actionType = fromOwnerUserId.HasValue
            ? toOwnerUserId.HasValue ? CrmTransferActionType.Transfer : CrmTransferActionType.Return
            : CrmTransferActionType.Assign;
        return new CrmTransferRecord(actionType, entityType, entityId, fromOwnerUserId, toOwnerUserId, operatorUserId, remark);
    }

    private static void EnsureEntity(string entityType, Guid entityId)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new BusinessException(400, "流转对象类型不能为空");
        if (entityId == Guid.Empty)
            throw new BusinessException(400, "流转对象不能为空");
    }
}




