namespace QPS.Shared.Storage;

/// <summary>
/// 文件存储抽象。上传控制器只依赖它，不关心落本地磁盘还是对象存储。
/// </summary>
public interface IFileStorage
{
    /// <summary>当前实现名，用于启动日志与自检。</summary>
    string ProviderName { get; }

    /// <summary>
    /// 保存文件。
    /// </summary>
    /// <param name="relativePath">相对路径，如 product/202611/xxx.png（不含 uploads 前缀）。</param>
    /// <param name="content">文件内容流。</param>
    /// <param name="contentType">MIME 类型，OSS 上作为对象元数据保存。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>可直接返回给前端的相对地址，如 /uploads/product/202611/xxx.png。</returns>
    Task<string> SaveAsync(
        string relativePath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);
}
