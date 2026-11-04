namespace QPS.Mall.Contracts.DTOs;

/// <summary>
/// 启用或停用商城门店请求。
/// </summary>
public class MallStoreStatusRequest
{
    public bool IsActive { get; set; }
}