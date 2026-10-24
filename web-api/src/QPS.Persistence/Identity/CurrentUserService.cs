using Microsoft.AspNetCore.Http;
using QPS.Shared.Contracts;

namespace QPS.Persistence.Identity;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirst("userId")?.Value;

    public string? Username => _httpContextAccessor.HttpContext?.User?.FindFirst("username")?.Value;
}


