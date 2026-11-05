using System;

namespace QPS.Shared.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
        IsDeleted = false;
    }

    /// <summary>
    /// 标记实体为软删除。查询侧由全局过滤器统一排除，业务代码不要重复写 IsDeleted 判断。
    /// </summary>
    public void SoftDelete()
    {
        IsDeleted = true;
    }

    /// <summary>
    /// 恢复已软删除的实体，仅用于数据初始化等需要查全量的场景。
    /// </summary>
    public void Restore()
    {
        IsDeleted = false;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || !(obj is BaseEntity))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        return Id == ((BaseEntity)obj).Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}