using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Shared.Storage;

namespace QPS.Mall.Controllers;

/// <summary>
/// 移动端会员图片上传：评价晒图、售后凭证。
/// 文件平铺存储，接口只返回文件名，URL 由前端自行拼接。
/// </summary>
[ApiController]
[Route("api/mall/file")]
[Authorize(Policy = "MallMember")]
public class MallMemberFileController : ControllerBase
{
    private readonly IFileStorage _storage;

    public MallMemberFileController(IFileStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// 上传评价/售后图片，仅返回文件名。
    /// </summary>
    [HttpPost("upload/img")]
    [RequestSizeLimit(ImageUploadValidator.MaxFileSize + 1024 * 1024)]
    public async Task<ActionResult<object>> UploadImage(
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        var (photo, extension) = ImageUploadValidator.Validate(file);
        var fileName = ImageUploadValidator.BuildFileName(extension);

        await using var stream = photo.OpenReadStream();
        await _storage.SaveAsync(fileName, stream, photo.ContentType, cancellationToken);

        return Ok(new { fileUrl = fileName });
    }
}
