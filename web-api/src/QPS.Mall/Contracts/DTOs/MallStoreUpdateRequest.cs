namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 编辑商场门店请求。
/// </summary>
public class MallStoreUpdateRequest
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
}
