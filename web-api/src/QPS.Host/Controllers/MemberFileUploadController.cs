using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Shared.Exceptions;

namespace QPS.Host.Controllers;

/// <summary>
/// 移动端会员图片上传。
/// </summary>
[ApiController]
[Route("api/mall/file")]
[Authorize(Policy = "MallMember")]
public class MemberFileUploadController : ControllerBase
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

    private static readonly HashSet<string> AllowedScenes = new(StringComparer.OrdinalIgnoreCase)
    {
        "review",
        "aftersale"
    };

    private readonly IWebHostEnvironment _environment;

    public MemberFileUploadController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// 上传评价图片，返回可直接访问的相对地址。
    /// </summary>
    [HttpPost("upload/img")]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    public async Task<ActionResult<object>> UploadImage(IFormFile? file, string scene = "review")
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

        if (!AllowedScenes.Contains(scene))
        {
            scene = "review";
        }

        var webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
        var relativeFolder = Path.Combine("uploads", scene, DateTime.Now.ToString("yyyyMM"));
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
