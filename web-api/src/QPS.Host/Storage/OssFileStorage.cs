using Aliyun.OSS;
using Aliyun.OSS.Common;
using Microsoft.Extensions.Options;
using QPS.Shared.Exceptions;
using QPS.Shared.Storage;

namespace QPS.Host.Storage;

/// <summary>
/// 阿里云 OSS 存储：对象键为 uploads/{scene}/{yyyyMM}/{guid}.ext，与本地目录结构一致。
/// 返回给前端的仍是 /uploads/... 相对地址，由前端拼上 VITE_FILE_BASE_URL 访问。
/// Bucket 可保持私有，上传后按 ObjectAcl 给单个对象授权（默认 public-read），
/// 这样浏览器能匿名读图，又不必把整个 Bucket 开放出去。
/// </summary>
public sealed class OssFileStorage : IFileStorage, IDisposable
{
    private readonly IOss _client;
    private readonly string _bucketName;
    private readonly string _objectPrefix;
    private readonly string _urlPrefix;
    private readonly CannedAccessControlList? _objectAcl;
    private readonly ILogger<OssFileStorage> _logger;

    public OssFileStorage(
        IOptions<FileStorageOptions> options,
        ILogger<OssFileStorage> logger)
    {
        var storageOptions = options.Value;
        var ossOptions = storageOptions.Oss;

        _logger = logger;
        _bucketName = ossOptions.BucketName;

        // 对象键前缀去掉两端斜杠，避免出现 //uploads/。
        _objectPrefix = ossOptions.Prefix.Trim('/');
        _urlPrefix = LocalFileStorage.NormalizePrefix(storageOptions.UrlPrefix, "/uploads");
        _objectAcl = MapObjectAcl(ossOptions.ObjectAcl);

        // SDK 接受裸域名，这里统一取 Host，防止配置里误写 http:// 或结尾斜杠。
        var endpoint = ossOptions.Endpoint.Trim();
        var endpointHost = new Uri(endpoint.Contains("://", StringComparison.Ordinal) ? endpoint : $"https://{endpoint}").Host;

        _client = new OssClient(endpointHost, ossOptions.AccessKeyId, ossOptions.AccessKeySecret);
    }

    public string ProviderName => "Oss";

    public async Task<string> SaveAsync(
        string relativePath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var segments = LocalFileStorage.NormalizeSegments(relativePath);
        var objectKey = string.IsNullOrEmpty(_objectPrefix)
            ? string.Join('/', segments)
            : $"{_objectPrefix}/{string.Join('/', segments)}";

        var metadata = new ObjectMetadata { ContentType = contentType };

        try
        {
            // SDK 只提供同步 PutObject 与 APM 接口，没有 Task 版方法：
            // 放到线程池执行，避免阻塞请求线程（单张图 ≤5MB，走单请求上传，不触发分片）。
            await Task.Run(() =>
            {
                _client.PutObject(_bucketName, objectKey, content, metadata);
                ApplyObjectAcl(objectKey);
            }, cancellationToken);
        }
        catch (OssException ex)
        {
            _logger.LogError(ex, "OSS 上传失败，bucket={Bucket}，key={Key}，错误码={ErrorCode}", _bucketName, objectKey, ex.ErrorCode);
            throw new BusinessException(500, "图片上传失败，请稍后重试", ex);
        }

        return $"{_urlPrefix}/{string.Join('/', segments)}";
    }

    /// <summary>
    /// 给单个对象单独授权，使私有 Bucket 下的图片也能被浏览器匿名读取。
    /// 权限不足（缺 oss:PutObjectAcl）时只告警，不影响上传本身。
    /// </summary>
    private void ApplyObjectAcl(string objectKey)
    {
        if (_objectAcl is null) return;

        try
        {
            _client.SetObjectAcl(_bucketName, objectKey, _objectAcl.Value);
        }
        catch (OssException ex)
        {
            _logger.LogWarning(ex, "OSS 对象 ACL 设置失败，图片可能无法匿名访问；bucket={Bucket}，key={Key}，错误码={ErrorCode}", _bucketName, objectKey, ex.ErrorCode);
        }
    }

    private static CannedAccessControlList? MapObjectAcl(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "public-read" => CannedAccessControlList.PublicRead,
        "public-read-write" => CannedAccessControlList.PublicReadWrite,
        _ => null // private / default / 空：不额外设置，跟随 Bucket ACL
    };

    public void Dispose() => (_client as IDisposable)?.Dispose();
}
