namespace QPS.Mall.Application.Features.MallOrders;

/// <summary>
/// 商场单据号生成：前缀 + 时间 + 随机数，配合数据库唯一索引使用。
/// </summary>
internal static class MallNumberGenerator
{
    public static string Generate(string prefix)
    {
        return $"{prefix}{DateTime.Now:yyyyMMddHHmmssfff}{Random.Shared.Next(1000, 10000)}";
    }

    public static string GenerateTransactionNo()
    {
        return $"MOCK{DateTime.Now:yyyyMMddHHmmssfff}{Random.Shared.Next(1000, 10000)}";
    }
}
