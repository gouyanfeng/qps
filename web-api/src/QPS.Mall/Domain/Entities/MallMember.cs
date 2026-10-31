using QPS.Shared.Common;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain.Entities;

public class MallMember : BaseEntity
{
    public string Phone { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public Guid? LevelId { get; private set; }
    public string Nickname { get; private set; } = string.Empty;
    public string Avatar { get; private set; } = string.Empty;
    public string? Birthday { get; private set; }
    public string? Gender { get; private set; }
    public int TotalPoints { get; private set; }
    public int AvailablePoints { get; private set; }
    public decimal TotalSpent { get; private set; }
    public int TotalOrders { get; private set; }
    public DateTime? LastConsumeAt { get; private set; }
    public DateTime? JoinAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public bool IsActive { get; private set; }

    private MallMember()
    {
    }

    private MallMember(string phone, string passwordHash, string nickname)
    {
        Phone = phone;
        PasswordHash = passwordHash;
        Nickname = nickname;
        TotalPoints = 0;
        AvailablePoints = 0;
        TotalSpent = 0;
        TotalOrders = 0;
        IsActive = true;
        JoinAt = DateTime.Now;
    }

    public static MallMember Register(string phone, string passwordHash, string? nickname = null)
    {
        var name = nickname ?? $"用户{phone.Substring(phone.Length - 4)}";
        return new MallMember(phone, passwordHash, name);
    }

    public bool VerifyPassword(string passwordHash)
    {
        return PasswordHash == passwordHash;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
    }

    public void MarkLogin()
    {
        LastLoginAt = DateTime.Now;
    }

    public void SetLevel(Guid levelId)
    {
        LevelId = levelId;
    }

    public void UpdateProfile(string? nickname, string? avatar, string? birthday, string? gender)
    {
        if (!string.IsNullOrWhiteSpace(nickname)) Nickname = nickname;
        if (!string.IsNullOrWhiteSpace(avatar)) Avatar = avatar;
        Birthday = birthday;
        Gender = gender;
    }

    public void AddPoints(int points)
    {
        if (points <= 0) throw new BusinessException(400, "积分必须大于0");
        TotalPoints += points;
        AvailablePoints += points;
    }

    public bool TryConsumePoints(int points)
    {
        if (points <= 0) return false;
        if (AvailablePoints < points) return false;
        AvailablePoints -= points;
        return true;
    }

    public void AddConsumeRecord(decimal amount)
    {
        TotalSpent += amount;
        TotalOrders += 1;
        LastConsumeAt = DateTime.Now;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}