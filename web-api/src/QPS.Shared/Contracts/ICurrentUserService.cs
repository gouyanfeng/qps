namespace QPS.Shared.Contracts;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Username { get; }
}


