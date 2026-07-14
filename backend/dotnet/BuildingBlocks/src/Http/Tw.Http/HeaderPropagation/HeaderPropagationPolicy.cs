using System.Collections.Frozen;

namespace Tw.Http.HeaderPropagation;

/// <summary>
/// 标识请求头值进入当前出站调用前经过的信任校验级别
/// </summary>
public enum HeaderTrustLevel
{
    /// <summary>
    /// 请求头值直接来自调用方且未经服务端验证
    /// </summary>
    ClientSupplied,

    /// <summary>
    /// 请求头值已经由可信服务端边界验证
    /// </summary>
    Verified
}

/// <summary>
/// 按平台安全基线和调用方允许列表选择出站请求头
/// </summary>
public static class HeaderPropagationPolicy
{
    /// <summary>
    /// 平台允许跨出站调用边界复制的请求头名称
    /// </summary>
    private static readonly FrozenSet<string> SafeHeaders = new[]
    {
        "traceparent",
        "tracestate",
        "X-Correlation-Id",
        "X-Tenant-Id",
        "X-Culture",
        "Idempotency-Key"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 即使调用方显式配置也不得自动复制的身份凭据与 Cookie 请求头
    /// </summary>
    private static readonly FrozenSet<string> SensitiveHeaders = new[]
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "Proxy-Authorization"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 判断请求头是否满足平台传播安全基线
    /// </summary>
    /// <param name="headerName">需要检查的请求头名称</param>
    /// <param name="trustLevel">请求头值进入当前出站调用前的信任级别</param>
    /// <returns>平台允许在指定信任级别传播时返回 <see langword="true"/></returns>
    /// <exception cref="ArgumentOutOfRangeException">trustLevel 未定义时抛出</exception>
    public static bool ShouldPropagate(string headerName, HeaderTrustLevel trustLevel)
    {
        EnsureDefinedTrustLevel(trustLevel);

        if (string.IsNullOrWhiteSpace(headerName) || SensitiveHeaders.Contains(headerName))
        {
            return false;
        }

        return SafeHeaders.Contains(headerName)
            && (!string.Equals(headerName, "X-Tenant-Id", StringComparison.OrdinalIgnoreCase)
                || trustLevel == HeaderTrustLevel.Verified);
    }

    /// <summary>
    /// 从输入快照中选择调用方配置且平台允许传播的请求头
    /// </summary>
    /// <param name="headers">当前请求提供的请求头名称和值</param>
    /// <param name="options">调用方明确允许传播的请求头名称</param>
    /// <param name="trustLevel">请求头值进入当前出站调用前的信任级别</param>
    /// <returns>不区分大小写且每个值列表均不可修改的出站请求头快照</returns>
    /// <exception cref="ArgumentNullException">headers 或 options 为 null 时抛出</exception>
    /// <exception cref="ArgumentException">输入包含 null 值或仅大小写不同的可传播同名请求头时抛出</exception>
    /// <exception cref="ArgumentOutOfRangeException">trustLevel 未定义时抛出</exception>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> SelectHeaders(
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers,
        HeaderPropagationOptions options,
        HeaderTrustLevel trustLevel)
    {
        if (headers is null)
        {
            throw new ArgumentNullException(nameof(headers), "请求头集合不能为空");
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options), "请求头传播选项不能为空");
        }

        EnsureDefinedTrustLevel(trustLevel);

        var selectedHeaders = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (headerName, headerValues) in headers)
        {
            if (headerValues is null)
            {
                throw new ArgumentException("请求头值集合不能为空", nameof(headers));
            }

            var headerValueSnapshot = new string[headerValues.Count];
            for (var index = 0; index < headerValues.Count; index++)
            {
                var headerValue = headerValues[index];
                if (headerValue is null)
                {
                    throw new ArgumentException("请求头值不能为空", nameof(headers));
                }

                headerValueSnapshot[index] = headerValue;
            }

            if (!options.AllowedHeaders.Contains(headerName)
                || !ShouldPropagate(headerName, trustLevel))
            {
                continue;
            }

            if (!selectedHeaders.TryAdd(headerName, Array.AsReadOnly(headerValueSnapshot)))
            {
                throw new ArgumentException("请求头名称不得仅因大小写不同而重复", nameof(headers));
            }
        }

        return selectedHeaders.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 拒绝未定义的信任级别以避免新增枚举值被默认放行
    /// </summary>
    /// <param name="trustLevel">需要验证的请求头信任级别</param>
    /// <exception cref="ArgumentOutOfRangeException">trustLevel 未定义时抛出</exception>
    private static void EnsureDefinedTrustLevel(HeaderTrustLevel trustLevel)
    {
        if (!Enum.IsDefined(trustLevel))
        {
            throw new ArgumentOutOfRangeException(
                nameof(trustLevel),
                trustLevel,
                "请求头信任级别不受支持");
        }
    }
}
