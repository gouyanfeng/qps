namespace QPS.System.Domain;

/// <summary>
/// 权限类型，与项目现有的状态常量写法保持一致（字符串列，便于迁移回填与直接排查数据）。
/// </summary>
public static class PermissionTypes
{
    /// <summary>菜单（页面/路由级）。</summary>
    public const string Menu = "Menu";

    /// <summary>按钮（操作级）。</summary>
    public const string Button = "Button";
}
