using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QPS.Shared.Storage;

namespace QPS.Mall.Controllers;

/// <summary>
/// 管理端图片上传。文件平铺存储，接口只返回文件名，URL 由前端自行拼接。
/// 缩略图不在此生成：列表等小尺寸场景由前端走阿里云 OSS 图片处理（IMG）实时缩放。
/// 存储方式由 Storage:Provider 决定；要求 admin 角色。
/// </summary>
[ApiController]
[Route("api/admin/file")]
[Authorize(Policy = "AdminOnly")]
public class MallFileController : ControllerBase
{
    private readonly IFileStorage _storage;

    public MallFileController(IFileStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// 上传图片：原图平铺保存，仅返回文件名。
    /// </summary>
    [HttpPost("upload/img")]
    [RequestSizeLimit(ImageUploadValidator.MaxFileSize + 1024 * 1024)]
    public async Task<ActionResult<object>> UploadImage(
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        var (photo, extension) = ImageUploadValidator.Validate(file);

        var fileName = ImageUploadValidator.BuildFileName(extension);

        await using var source = new MemoryStream();
        await photo.CopyToAsync(source, cancellationToken);

        source.Position = 0;
        await _storage.SaveAsync(fileName, source, photo.ContentType, cancellationToken);

        return Ok(new { fileUrl = fileName });
    }
}
