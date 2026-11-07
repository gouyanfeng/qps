using Microsoft.EntityFrameworkCore;
using QPS.Mall.Contracts;
using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Application.Features.MallCategories;

/// <summary>
/// 商品分类字段规范化与校验。
/// </summary>
internal static class MallCategoryRules
{
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 100;
    public const int MaxSortOrder = 9999;

    public static string NormalizeCode(string? code)
    {
        var normalized = code?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessException(400, "请输入分类编码");
        }

        if (normalized.Length > CodeMaxLength)
        {
            throw new BusinessException(400, $"分类编码不能超过 {CodeMaxLength} 个字符");
        }

        return normalized;
    }

    public static string NormalizeName(string? name)
    {
        var normalized = name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessException(400, "请输入分类名称");
        }

        if (normalized.Length > NameMaxLength)
        {
            throw new BusinessException(400, $"分类名称不能超过 {NameMaxLength} 个字符");
        }

        return normalized;
    }

    public static void ValidateSortOrder(int sortOrder)
    {
        if (sortOrder < 0 || sortOrder > MaxSortOrder)
        {
            throw new BusinessException(400, $"排序值必须在 0 到 {MaxSortOrder} 之间");
        }
    }

    /// <summary>
    /// 确认分类编码未被其他分类占用。
    /// </summary>
    public static async Task EnsureCodeNotDuplicatedAsync(
        IMallDbContext dbContext,
        string code,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var duplicated = await dbContext.MallCategories.AnyAsync(
            category =>
                category.Code == code &&
                (!excludeId.HasValue || category.Id != excludeId.Value),
            cancellationToken);

        if (duplicated)
        {
            throw new BusinessException(400, "分类编码已存在");
        }
    }

    public static async Task EnsureParentValidAsync(
        IMallDbContext dbContext,
        Guid? parentId,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            if (excludeId.HasValue)
            {
                var hasChildren = await dbContext.MallCategories.AnyAsync(
                    category => category.ParentId == excludeId.Value,
                    cancellationToken);
                if (hasChildren)
                {
                    throw new BusinessException(400, "请先调整子分类后再设置为顶级分类");
                }
            }

            return;
        }

        if (excludeId == parentId)
        {
            throw new BusinessException(400, "分类不能以自身作为父级");
        }

        var parent = await dbContext.MallCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                category => category.Id == parentId.Value,
                cancellationToken);
        if (parent is null || !parent.IsActive)
        {
            throw new BusinessException(400, "请选择已启用的父级分类");
        }

        if (parent.ParentId.HasValue)
        {
            throw new BusinessException(400, "分类仅支持两级结构，父级必须为顶级分类");
        }

        if (excludeId.HasValue)
        {
            var hasChildren = await dbContext.MallCategories.AnyAsync(
                category => category.ParentId == excludeId.Value,
                cancellationToken);
            if (hasChildren)
            {
                throw new BusinessException(400, "请先调整子分类后再设置父级");
            }
        }
    }
}