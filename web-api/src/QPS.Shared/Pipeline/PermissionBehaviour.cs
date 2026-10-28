using System.Collections.Concurrent;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using QPS.Shared.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Shared.Pipeline;

public class PermissionBehaviour<TRequest, TResponse>(
    IPermissionChecker permissionChecker,
    ICurrentUserService currentUserService,
    IHttpContextAccessor httpContextAccessor) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (typeof(IAllowAnonymousRequest).IsAssignableFrom(typeof(TRequest)) ||
            httpContextAccessor.HttpContext?.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            return await next(cancellationToken);
        }

        if (!Guid.TryParse(currentUserService.UserId, out var userId) || userId == Guid.Empty)
        {
            throw new BusinessException(401, "登录状态无效");
        }

        if (!await permissionChecker.IsUserActiveAsync(userId, cancellationToken))
        {
            throw new BusinessException(401, "当前用户不可用");
        }

        var permissionCode = PermissionMetadataCache.GetPermissionCode(typeof(TRequest));
        if (permissionCode != null &&
            !await permissionChecker.HasPermissionAsync(userId, permissionCode, cancellationToken))
        {
            throw new BusinessException(403, "无权访问该功能");
        }

        return await next(cancellationToken);
    }
}

internal static class PermissionMetadataCache
{
    private static readonly ConcurrentDictionary<Type, string?> PermissionCodes = new();

    public static string? GetPermissionCode(Type requestType) =>
        PermissionCodes.GetOrAdd(
            requestType,
            type => type.GetCustomAttribute<PermissionAttribute>()?.PermissionCode);
}
