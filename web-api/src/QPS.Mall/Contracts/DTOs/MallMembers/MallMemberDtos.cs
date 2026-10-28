namespace QPS.Mall.Contracts.DTOs.MallMembers;

public class MallMemberDto
{
    public Guid Id { get; set; }
    public string Phone { get; set; } = string.Empty;
    public Guid? StoreId { get; set; }
    public Guid? LevelId { get; set; }
    public string LevelName { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string? Birthday { get; set; }
    public string? Gender { get; set; }
    public int TotalPoints { get; set; }
    public int AvailablePoints { get; set; }
    public decimal TotalSpent { get; set; }
    public int TotalOrders { get; set; }
    public DateTime? LastConsumeAt { get; set; }
    public DateTime? JoinAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
}

public class MallMemberCreateRequest
{
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Guid? StoreId { get; set; }
    public string Nickname { get; set; } = string.Empty;
}

public class MallMemberUpdateRequest
{
    public string? Nickname { get; set; }
    public string? Avatar { get; set; }
    public string? Birthday { get; set; }
    public string? Gender { get; set; }
    public Guid? LevelId { get; set; }
}

public class MallMemberQueryRequest
{
    public Guid? StoreId { get; set; }
    public Guid? LevelId { get; set; }
    public bool? IsActive { get; set; }
    public string? Keyword { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class MallMemberAdjustPointsRequest
{
    public int Points { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
}

public class MallMemberRegisterRequest
{
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public Guid? StoreId { get; set; }
}

public class MallMemberLoginRequest
{
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class MallMemberAuthResponse
{
    public string Token { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Role { get; set; } = "member";
    public int AvailablePoints { get; set; }
    public Guid? LevelId { get; set; }
}