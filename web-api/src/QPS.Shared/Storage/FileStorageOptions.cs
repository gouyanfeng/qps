namespace QPS.Shared.Storage;

/// <summary>
/// 文件存储配置。默认 Local（写入 wwwroot），配置 OSS 后切换为对象存储。
/// 上传接口返回的相对地址始终保持 /uploads/... 不变，因此前端无需感知存储位置。
/// </summary>
public sealed class FileStorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>存储方式：Local / Oss。</summary>
    public string Provider { get; init; } = "Local";

    /// <summary>本地存储根目录，相对 ContentRoot。</summary>
    public string LocalRoot { get; init; } = "wwwroot";

    /// <summary>对外暴露的 URL 前缀，与前端拼接的文件域名共同构成完整地址。</summary>
    public string UrlPrefix { get; init; } = "/uploads";

    public OssStorageOptions Oss { get; init; } = new();

    public bool UseOss => string.Equals(Provider, "Oss", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// 阿里云 OSS 连接参数。AccessKey 建议使用只有该 Bucket 写权限的 RAM 子账号。
/// </summary>
public sealed class OssStorageOptions
{
    /// <summary>区域 Endpoint，如 oss-cn-hangzhou.aliyuncs.com（带不带 https:// 均可）。</summary>
    public string Endpoint { get; init; } = string.Empty;

    public string BucketName { get; init; } = string.Empty;

    public string AccessKeyId { get; init; } = string.Empty;

    public string AccessKeySecret { get; init; } = string.Empty;

    /// <summary>对象键前缀，落到 OSS 上就是 uploads/product/202611/xxx.png。</summary>
    public string Prefix { get; init; } = "uploads";

    /// <summary>
    /// 上传后给对象单独设置的 ACL：public-read（默认）/ private / default（不设置，跟随 Bucket）。
    /// 默认 public-read 是因为 Bucket 通常保持私有，只有图片对象需要被浏览器匿名读取。
    /// </summary>
    public string ObjectAcl { get; init; } = "public-read";
}
