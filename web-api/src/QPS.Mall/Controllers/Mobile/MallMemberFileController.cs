using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QPS.Shared.Storage;

namespace QPS.Mall.Controllers;

/// <summary>
/// 移动端会员图片上传：评价晒图、售后凭证。
/// </summary>
[ApiController]
[Route("api/mall/file")]
[Authorize(Policy = "MallMember")]
public class MallMemberFileController : ControllerBase
{
    private static readonly HashSet<string> AllowedScenes = new(StringComparer.OrdinalIgnoreCase)
    {
        "review",
        "aftersale"
    };

    private readonly IFileStorage _storage;

    public MallMemberFileController(IFileStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// 上传评价图片，返回可直接访问的相对地址。
    /// </summary>
    [HttpPost("upload/img")]
    [RequestSizeLimit(ImageUploadValidator.MaxFileSize + 1024 * 1024)]
    public async Task<ActionResult<object>> UploadImage(
        IFormFile? file,
        string scene = "review",
        CancellationToken cancellationToken = default)
    {
        var (photo, extension) = ImageUploadValidator.Validate(file);

        var normalizedScene = ImageUploadValidator.NormalizeScene(scene, AllowedScenes, "review");
        var relativePath = ImageUploadValidator.BuildRelativePath(normalizedScene, extension);

        await using var stream = photo.OpenReadStream();
        var fileUrl = await _storage.SaveAsync(relativePath, stream, photo.ContentType, cancellationToken);

        return Ok(new { fileUrl });
    }
}
