namespace QPS.Shared.Contracts;

/// <summary>
/// 权限校验契约，由 System 模块实现，公共管线只依赖该抽象。
/// </summary>
public interface IPermissionChecker
{
    Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default);
}
