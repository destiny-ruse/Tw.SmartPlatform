using System.Globalization;

namespace Tw.Localization;

/// <summary>
/// 提供文化回退链的展开与校验工具方法
/// </summary>
public static class CultureFallback
{
    /// <summary>
    /// 判断给定的 BCP 47 文化名称是否合法
    /// </summary>
    /// <param name="cultureName">要校验的文化名称</param>
    /// <returns>当 <paramref name="cultureName"/> 对应一个已知文化时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    public static bool IsValidCulture(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return false;
        }

        try
        {
            CultureInfo.GetCultureInfo(cultureName);
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// 根据上下文和系统选项展开完整的文化回退链，结果不包含重复项，顺序为：当前文化、父文化链、默认文化
    /// </summary>
    /// <param name="context">本次查找的本地化上下文</param>
    /// <param name="options">系统级本地化配置</param>
    /// <returns>有序且去重的文化名称只读列表</returns>
    /// <exception cref="ArgumentNullException">当 <paramref name="context"/> 或 <paramref name="options"/> 为 <see langword="null"/> 时抛出</exception>
    public static IReadOnlyList<string> Expand(LocalizationContext context, LocalizationOptions options)
    {
        Check.NotNull(context);
        Check.NotNull(options);

        var result = new List<string>();
        AddIfMissing(result, context.CultureName);

        if (context.FallbackToParentCultures && options.FallbackToParentCultures)
        {
            var culture = CultureInfo.GetCultureInfo(context.CultureName);
            while (!string.IsNullOrWhiteSpace(culture.Parent.Name))
            {
                culture = culture.Parent;
                if (options.SupportedCultures.Contains(culture.Name, StringComparer.OrdinalIgnoreCase))
                {
                    AddIfMissing(result, culture.Name);
                }
            }
        }

        if (context.FallbackToDefaultCulture && options.FallbackToDefaultCulture)
        {
            AddIfMissing(result, options.DefaultCulture);
        }

        return result;
    }

    private static void AddIfMissing(List<string> cultures, string cultureName)
    {
        if (!cultures.Contains(cultureName, StringComparer.OrdinalIgnoreCase))
        {
            cultures.Add(cultureName);
        }
    }
}
