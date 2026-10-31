namespace QPS.Mall.Contracts.DTOs.MallMembers;

public class MallMemberLevelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int LevelCode { get; set; }
    public decimal? DiscountRate { get; set; }
    public int MinSpent { get; set; }
    public int MaxSpent { get; set; }
    public int UpgradePoints { get; set; }
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}

public class MallMemberLevelCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public int LevelCode { get; set; }
    public decimal? DiscountRate { get; set; }
    public int MinSpent { get; set; }
    public int MaxSpent { get; set; }
    public int UpgradePoints { get; set; }
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
}

public class MallMemberLevelUpdateRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal? DiscountRate { get; set; }
    public int MinSpent { get; set; }
    public int MaxSpent { get; set; }
    public int UpgradePoints { get; set; }
    public int SortOrder { get; set; }
}

public class MallMemberLevelQueryRequest
{
    public bool? IsActive { get; set; }
}