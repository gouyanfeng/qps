using Microsoft.Extensions.Options;
using QPS.Shared.Storage;

namespace QPS.Host.Storage;

/// <summary>
/// 本地磁盘存储：写入 ContentRoot/wwwroot/uploads，由 UseStaticFiles 直接对外提供。
/// 容器部署时该目录挂在 qps-uploads 数据卷上，重建容器不丢文件。
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _webRootPath;
    private readonly string _urlPrefix;

    public LocalFileStorage(IWebHostEnvironment environment, IOptions<FileStorageOptions> options)
    {
        var storageOptions = options.Value;
        _webRootPath = Path.Combine(environment.ContentRootPath, storageOptions.LocalRoot);
        _urlPrefix = NormalizePrefix(storageOptions.UrlPrefix, "/uploads");
        Directory.CreateDirectory(_webRootPath);
    }

    public string ProviderName => "Local";

    public async Task<string> SaveAsync(
        string relativePath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var relativeSegments = NormalizeSegments(relativePath);

        // 磁盘目录与对外 URL 一一对应：/uploads/... 落在 wwwroot/uploads/...。
        var targetFolder = Path.Combine(
            _webRootPath,
            _urlPrefix.Trim('/'),
            Path.Combine(relativeSegments[..^1]));
        Directory.CreateDirectory(targetFolder);

        var targetPath = Path.Combine(targetFolder, relativeSegments[^1]);

        await using var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(stream, cancellationToken);

        return $"{_urlPrefix}/{string.Join('/', relativeSegments)}";
    }

    internal static string NormalizePrefix(string? value, string fallback)
    {
        var prefix = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return $"/{prefix.Trim('/')}";
    }

    /// <summary>
    /// 统一按 "/" 拆分，避免 Linux 容器与 Windows 本地在路径分隔符上的差异。
    /// </summary>
    internal static string[] NormalizeSegments(string relativePath)
    {
        var segments = relativePath
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 2 || segments.Any(segment => segment is "." or ".."))
        {
            throw new ArgumentException($"非法的相对路径：{relativePath}", nameof(relativePath));
        }

        return segments;
    }
}
