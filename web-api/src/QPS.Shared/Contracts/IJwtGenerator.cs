namespace QPS.Shared.Contracts;

public interface IJwtGenerator
{
    string GenerateToken(Guid userId, string username, string role);
}


