using QPS.Shared.Contracts;

namespace QPS.Shared.Extensions;

public static class CurrentUserExtensions
{
    public static Guid GetUserId(this ICurrentUserService currentUser)
    {
        if (!Guid.TryParse(currentUser.UserId, out var id) || id == Guid.Empty)
            throw new UnauthorizedAccessException("用户未登录");
        return id;
    }

    public static bool IsAuthenticated(this ICurrentUserService currentUser)
    {
        return Guid.TryParse(currentUser.UserId, out var id) && id != Guid.Empty;
    }
}