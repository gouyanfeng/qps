using QPS.Shared.Contracts;

namespace QPS.Shared.Extensions;

public static class CurrentUserExtensions
{
    public static Guid GetUserId(this ICurrentUserService currentUser)
    {
        if (Guid.TryParse(currentUser.UserId, out var id) && id != Guid.Empty)
            return id;
        throw new QPS.Shared.Exceptions.BusinessException(401, "登录状态无效");
    }

    public static bool IsAuthenticated(this ICurrentUserService currentUser)
    {
        return Guid.TryParse(currentUser.UserId, out var id) && id != Guid.Empty;
    }
}