using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QPS.Shared.Storage;

namespace QPS.Mall.Controllers;

/// <summary>
/// 管理端图片上传。服务于商品、分类、Banner、活动等 mall 内容维护。
/// 与 Mall 其他 Admin 控制器一致要求 admin 角色；存储方式由 Storage:Provider 决定。
/// </summary>
[ApiController]
[Route("api/admin/file")]
[Authorize(Policy = "AdminOnly")]
public class MallFileController : ControllerBase
{
    private static readonly HashSet<string> AllowedScenes = new(StringComparer.OrdinalIgnoreCase)
    {
        "product", "category", "banner", "activity", "avatar", "common"
    };

    private readonly IFileStorage _storage;

    public MallFileController(IFileStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// 上传图片，返回可直接访问的相对地址。落本地还是 OSS 由 Storage:Provider 决定。
    /// </summary>
    /// <param name="file">图片文件</param>
    /// <param name="scene">场景：product / category / banner / activity / avatar / common</param>
    [HttpPost("upload/img")]
    [RequestSizeLimit(ImageUploadValidator.MaxFileSize + 1024 * 1024)]
    public async Task<ActionResult<object>> UploadImage(
        IFormFile? file,
        string scene = "common",
        CancellationToken cancellationToken = default)
    {
        var (photo, extension) = ImageUploadValidator.Validate(file);

        var normalizedScene = ImageUploadValidator.NormalizeScene(scene, AllowedScenes, "common");
        var relativePath = ImageUploadValidator.BuildRelativePath(normalizedScene, extension);

        await using var stream = photo.OpenReadStream();
        var fileUrl = await _storage.SaveAsync(relativePath, stream, photo.ContentType, cancellationToken);

        return Ok(new { fileUrl });
    }
}
