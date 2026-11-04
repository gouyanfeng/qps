using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallStores;

/// <summary>
/// 商城门店字段规范化与校验。
/// </summary>
internal static class MallStoreRules
{
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 100;
    public const int AddressMaxLength = 200;

    public static string NormalizeCode(string? code)
    {
        var normalized = code?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessException(400, "请输入门店编码");
        }

        if (normalized.Length > CodeMaxLength)
        {
            throw new BusinessException(400, $"门店编码不能超过 {CodeMaxLength} 个字符");
        }

        return normalized;
    }

    public static string NormalizeName(string? name)
    {
        var normalized = name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessException(400, "请输入门店名称");
        }

        if (normalized.Length > NameMaxLength)
        {
            throw new BusinessException(400, $"门店名称不能超过 {NameMaxLength} 个字符");
        }

        return normalized;
    }

    public static string NormalizeAddress(string? address)
    {
        var normalized = address?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessException(400, "请输入门店地址");
        }

        if (normalized.Length > AddressMaxLength)
        {
            throw new BusinessException(400, $"门店地址不能超过 {AddressMaxLength} 个字符");
        }

        return normalized;
    }

    /// <summary>
    /// 确认门店编码未被其他门店占用。
    /// </summary>
    public static async Task EnsureCodeNotDuplicatedAsync(
        IMallDbContext dbContext,
        string code,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var duplicated = await dbContext.MallStores.AnyAsync(
            store =>
                !store.IsDeleted &&
                store.Code == code &&
                (!excludeId.HasValue || store.Id != excludeId.Value),
            cancellationToken);

        if (duplicated)
        {
            throw new BusinessException(400, "门店编码已存在");
        }
    }
}