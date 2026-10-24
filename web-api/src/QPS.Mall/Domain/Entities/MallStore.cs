using QPS.Shared.Common;

namespace QPS.Mall.Domain.Entities;

/// <summary>
/// 商场门店。
/// </summary>
public class MallStore : BaseEntity
{
    /// <summary>
    /// 门店编码，用于唯一标识门店。
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// 门店名称。
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// 门店地址。
    /// </summary>
    public string Address { get; private set; } = string.Empty;

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool IsActive { get; private set; }

    private MallStore()
    {
    }

    private MallStore(string code, string name, string address, bool isActive)
    {
        Code = code;
        Name = name;
        Address = address;
        IsActive = isActive;
    }

    public static MallStore Create(string code, string name, string address, bool isActive)
    {
        return new MallStore(code, name, address, isActive);
    }

    public void Update(string code, string name, string address)
    {
        Code = code;
        Name = name;
        Address = address;
    }

    public void ChangeStatus(bool isActive)
    {
        IsActive = isActive;
    }
}
