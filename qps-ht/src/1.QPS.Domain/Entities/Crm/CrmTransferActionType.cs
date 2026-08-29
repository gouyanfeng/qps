namespace QPS.Domain.Entities.Crm;

public static class CrmTransferActionType
{
    public const string Entry = "ENTRY";
    public const string Assign = "ASSIGN";
    public const string Transfer = "TRANSFER";
    public const string Return = "RETURN";

    public static string GetName(string actionType) => actionType switch
    {
        Entry => "入库",
        Assign => "分配",
        Transfer => "转交",
        Return => "退回待分配池",
        _ => "未知流转"
    };
}
