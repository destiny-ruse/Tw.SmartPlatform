namespace Tw.AspNetCore.Mvc.Security;

/// <summary>
/// 封装防伪策略相关的数据和行为
/// </summary>
public static class AntiforgeryPolicy
{
    /// <summary>
    /// 保存当前类型处理流程依赖的SafeMethods
    /// </summary>
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET",
        "HEAD",
        "OPTIONS",
        "TRACE"
    };

    /// <summary>
    /// 判断请求是否需要执行防伪校验
    /// </summary>
    /// <param name="method">用于构造测试场景的方法元数据</param>
    /// <param name="authenticationScheme">当前请求使用的认证方案</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    public static bool RequiresValidation(string method, string authenticationScheme)
    {
        return !SafeMethods.Contains(method)
            && string.Equals(authenticationScheme, "Cookies", StringComparison.OrdinalIgnoreCase);
    }
}
