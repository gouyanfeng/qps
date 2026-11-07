using QPS.Shared.Common;

namespace QPS.System.Domain.Entities;

public class SystemPermission : BaseEntity
{
    public string Name { get; private set; }
    public string Code { get; private set; }
    public Guid? ParentId { get; private set; }
    public string PermissionType { get; private set; } = PermissionTypes.Menu;

    protected SystemPermission() { }

    public SystemPermission(string name, string code, Guid? parentId = null, string permissionType = PermissionTypes.Menu)
    {
        Name = name;
        Code = code;
        ParentId = parentId;
        PermissionType = permissionType;
    }

    public void Update(string name, string code, Guid? parentId = null)
    {
        Name = name;
        Code = code;
        ParentId = parentId;
    }
}

