using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

public class MallMemberPoint : BaseEntity
{
    public Guid MemberId { get; private set; }
    public int ChangeAmount { get; private set; }
    public int Balance { get; private set; }
    public string ChangeType { get; private set; } = string.Empty;
    public string? RefNo { get; private set; }
    public string Remark { get; private set; } = string.Empty;

    private MallMemberPoint()
    {
    }

    private MallMemberPoint(
        Guid memberId, int changeAmount, int balance,
        string changeType, string? refNo, string remark)
    {
        MemberId = memberId;
        ChangeAmount = changeAmount;
        Balance = balance;
        ChangeType = changeType;
        RefNo = refNo;
        Remark = remark;
    }

    public static MallMemberPoint Create(
        Guid memberId, int changeAmount, int balance,
        string changeType, string? refNo, string remark)
    {
        return new MallMemberPoint(memberId, changeAmount, balance, changeType, refNo, remark);
    }
}