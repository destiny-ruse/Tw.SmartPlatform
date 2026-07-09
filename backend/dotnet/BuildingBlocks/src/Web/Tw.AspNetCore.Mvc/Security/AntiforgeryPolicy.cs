namespace Tw.AspNetCore.Mvc.Security;

/// <summary>表示 AntiforgeryPolicy 类型</summary>
public static class AntiforgeryPolicy
{
    /// <summary>表示 SafeMethods 字段</summary>
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET",
        "HEAD",
        "OPTIONS",
        "TRACE"
    };

    /// <summary>执行 RequiresValidation 操作</summary>
    /// <param name="method">method 参数</param>
    /// <param name="authenticationScheme">authenticationScheme 参数</param>
    /// <returns>RequiresValidation 的执行结果</returns>
    public static bool RequiresValidation(string method, string authenticationScheme)
    {
        return !SafeMethods.Contains(method)
            && string.Equals(authenticationScheme, "Cookies", StringComparison.OrdinalIgnoreCase);
    }
}
