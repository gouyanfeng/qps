using QPS.Mall.Domain.Entities;
using QPS.Shared.Exceptions;

namespace QPS.Mall.Domain;

/// <summary>
/// 售后申请的公共约束：原因必填、凭证图片上限、签收后申请时效。
/// </summary>
public static class MallAfterSaleRules
{
    /// <summary>凭证图片最多张数。</summary>
    public const int EvidenceMaxCount = 6;

    /// <summary>已签收后允许申请售后的天数。</summary>
    public const int CompletionWindowDays = 7;

    /// <summary>补充说明最大长度。</summary>
    public const int DescriptionMaxLength = 500;

    private const int ReasonMaxLength = 200;

    /// <summary>原因必填，返回去除首尾空白后的文本。</summary>
    public static string RequireReason(string? reason)
    {
        var normalized = reason?.Trim() ?? string.Empty;

        if (normalized.Length == 0)
        {
            throw new BusinessException(400, "请选择申请原因");
        }

        if (normalized.Length > ReasonMaxLength)
        {
            throw new BusinessException(400, $"申请原因不能超过 {ReasonMaxLength} 个字符");
        }

        return normalized;
    }

    /// <summary>补充说明选填，空值统一存 null。</summary>
    public static string? NormalizeDescription(string? description)
    {
        var normalized = description?.Trim();

        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (normalized.Length > DescriptionMaxLength)
        {
            throw new BusinessException(400, $"补充说明不能超过 {DescriptionMaxLength} 个字符");
        }

        return normalized;
    }

    /// <summary>凭证图片入库为逗号分隔的相对地址，最多 6 张。</summary>
    public static string NormalizeEvidence(IEnumerable<string>? images)
    {
        var normalized = images?
            .Where(image => !string.IsNullOrWhiteSpace(image))
            .Select(image => image.Trim())
            .Distinct()
            .ToList() ?? new List<string>();

        if (normalized.Count > EvidenceMaxCount)
        {
            throw new BusinessException(400, $"凭证图片最多上传 {EvidenceMaxCount} 张");
        }

        return string.Join(",", normalized);
    }

    /// <summary>
    /// 已签收订单需在签收后 7 天内申请售后；待发货订单的仅退款不受时效限制。
    /// </summary>
    public static void EnsureWithinAfterSaleWindow(MallOrder order, DateTime now)
    {
        if (!order.CompletedAt.HasValue)
        {
            return;
        }

        if (now > order.CompletedAt.Value.AddDays(CompletionWindowDays))
        {
            throw new BusinessException(
                400,
                $"订单已签收超过 {CompletionWindowDays} 天，无法申请售后");
        }
    }

    /// <summary>凭证地址回显为数组。</summary>
    public static string[] SplitEvidence(string? evidenceImages)
    {
        if (string.IsNullOrWhiteSpace(evidenceImages))
        {
            return Array.Empty<string>();
        }

        return evidenceImages
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
