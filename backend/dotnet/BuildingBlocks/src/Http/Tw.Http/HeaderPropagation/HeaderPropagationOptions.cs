using System.Collections.Frozen;

namespace Tw.Http.HeaderPropagation;

/// <summary>
/// 声明调用方明确允许跨出站 HTTP 边界复制的请求头
/// </summary>
public sealed record HeaderPropagationOptions
{
    /// <summary>
    /// 复制允许列表并建立不区分大小写的只读快照
    /// </summary>
    /// <param name="allowedHeaders">调用方明确允许传播的请求头名称集合</param>
    /// <exception cref="ArgumentNullException">allowedHeaders 为 null 时抛出</exception>
    /// <exception cref="ArgumentException">集合包含空白请求头名称时抛出</exception>
    public HeaderPropagationOptions(IEnumerable<string> allowedHeaders)
    {
        if (allowedHeaders is null)
        {
            throw new ArgumentNullException(nameof(allowedHeaders), "允许传播的请求头集合不能为空");
        }

        var allowedHeaderSnapshot = allowedHeaders.ToArray();
        if (allowedHeaderSnapshot.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("允许传播的请求头名称不能为空", nameof(allowedHeaders));
        }

        AllowedHeaders = allowedHeaderSnapshot.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 不区分大小写且不受原始集合后续修改影响的允许列表
    /// </summary>
    public IReadOnlySet<string> AllowedHeaders { get; }
}
