using Microsoft.AspNetCore.Http;
using QPS.Shared.Exceptions;

namespace QPS.Shared.Storage;

/// <summary>
/// 图片上传校验：两个上传入口（管理端 / 会员端）共用，避免规则漂移。
/// </summary>
public static class ImageUploadValidator
{
    public const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string[]> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = new[] { "image/jpeg" },
        [".jpeg"] = new[] { "image/jpeg" },
        [".png"] = new[] { "image/png" },
        [".gif"] = new[] { "image/gif" },
        [".webp"] = new[] { "image/webp" }
    };

    /// <summary>
    /// 校验大小、扩展名与 MIME，返回已确认非空的文件与小写扩展名。
    /// 校验失败直接抛 BusinessException，调用方无需再判空。
    /// </summary>
    public static (IFormFile File, string Extension) Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            throw new BusinessException(400, "请选择要上传的图片");
        }

        if (file.Length > MaxFileSize)
        {
            throw new BusinessException(400, "图片大小不能超过 5MB");
        }

        var extension = Path.GetExtension(file.FileName);

        if (string.IsNullOrWhiteSpace(extension) ||
            !AllowedExtensions.TryGetValue(extension, out var allowedContentTypes))
        {
            throw new BusinessException(400, "仅支持 jpg、jpeg、png、gif、webp 格式的图片");
        }

        if (!allowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessException(400, "图片内容类型不合法");
        }

        return (file, extension.ToLowerInvariant());
    }

    /// <summary>
    /// 生成平铺文件名：{guid}{ext}，不含任何目录。
    /// 存储归一化与前端 URL 拼接各自负责前缀，文件名本身保持中性。
    /// </summary>
    public static string BuildFileName(string extension)
        => $"{Guid.NewGuid():N}{extension}";
}
