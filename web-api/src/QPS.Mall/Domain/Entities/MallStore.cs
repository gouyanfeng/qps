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

    /// <summary>
    /// 默认运费模板ID，商品未指定模板时使用。
    /// </summary>
    public Guid? DefaultFreightTemplateId { get; private set; }

    private MallStore()
    {
    }

    private MallStore(string code, string name, string address, bool isActive, Guid? defaultFreightTemplateId)
    {
        Code = code;
        Name = name;
        Address = address;
        IsActive = isActive;
        DefaultFreightTemplateId = defaultFreightTemplateId;
    }

    public static MallStore Create(string code, string name, string address, bool isActive, Guid? defaultFreightTemplateId = null)
    {
        return new MallStore(code, name, address, isActive, defaultFreightTemplateId);
    }

    public void Update(string code, string name, string address, Guid? defaultFreightTemplateId)
    {
        Code = code;
        Name = name;
        Address = address;
        DefaultFreightTemplateId = defaultFreightTemplateId;
    }

    public void ChangeStatus(bool isActive)
    {
        IsActive = isActive;
    }
}