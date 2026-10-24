using Microsoft.AspNetCore.Mvc;
using QPS.Shared.Exceptions;

namespace QPS.Host.Controllers;

/// <summary>
/// 管理端图片上传。
/// </summary>
[ApiController]
[Route("api/admin/file")]
public class FileUploadController : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string[]> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = new[] { "image/jpeg" },
        [".jpeg"] = new[] { "image/jpeg" },
        [".png"] = new[] { "image/png" },
        [".gif"] = new[] { "image/gif" },
        [".webp"] = new[] { "image/webp" }
    };

    private readonly IWebHostEnvironment _environment;

    public FileUploadController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// 上传商品图片，返回可直接访问的相对地址。
    /// </summary>
    [HttpPost("upload/img")]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    public async Task<ActionResult<object>> UploadImage(IFormFile? file)
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

        var webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        var relativeFolder = Path.Combine("uploads", DateTime.Now.ToString("yyyyMM"));
        var targetFolder = Path.Combine(webRootPath, relativeFolder);

        Directory.CreateDirectory(targetFolder);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var targetPath = Path.Combine(targetFolder, fileName);

        await using (var stream = new FileStream(targetPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var fileUrl = $"/{Path.Combine(relativeFolder, fileName).Replace('\\', '/')}";

        return Ok(new { fileUrl });
    }
}
