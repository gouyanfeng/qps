using Aliyun.OSS;
using Aliyun.OSS.Common;
using Microsoft.Extensions.Options;
using QPS.Shared.Exceptions;
using QPS.Shared.Storage;

namespace QPS.Host.Storage;

/// <summary>
/// 阿里云 OSS 存储：对象键为 uploads/{scene}/{yyyyMM}/{guid}.ext，与本地目录结构一致。
/// 返回给前端的仍是 /uploads/... 相对地址，由前端拼上 VITE_FILE_BASE_URL 访问。
/// Bucket 需为公共读，或把 PublicDomain 指向已绑定该 Bucket 的 CDN 域名。
/// </summary>
public sealed class OssFileStorage : IFileStorage, IDisposable
{
    private readonly IOss _client;
    private readonly string _bucketName;
    private readonly string _objectPrefix;
    private readonly string _urlPrefix;
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
            await Task.Run(() => _client.PutObject(_bucketName, objectKey, content, metadata), cancellationToken);
        }
        catch (OssException ex)
        {
            _logger.LogError(ex, "OSS 上传失败，bucket={Bucket}，key={Key}，错误码={ErrorCode}", _bucketName, objectKey, ex.ErrorCode);
            throw new BusinessException(500, "图片上传失败，请稍后重试", ex);
        }

        return $"{_urlPrefix}/{string.Join('/', segments)}";
    }

    public void Dispose() => (_client as IDisposable)?.Dispose();
}
