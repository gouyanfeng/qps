namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 新增商场门店请求。
/// </summary>
public class MallStoreCreateRequest
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
