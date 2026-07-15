using Serilog.Core;
using Serilog.Events;
using Tw.Security.DataMasking;

namespace Tw.Observability.Serilog;

/// <summary>
/// 对结构化日志中的受控敏感标量属性执行脱敏
/// </summary>
public sealed class RedactingLogEventEnricher(IDataMasker dataMasker) : ILogEventEnricher
{
    /// <summary>
    /// 不含分隔符时允许精确匹配的敏感属性名
    /// </summary>
    private static readonly HashSet<string> CompactSensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "secret",
        "token",
        "connectionstring",
        "apikey",
        "authorization",
        "credential",
        "privatekey",
        "cookie"
    };

    /// <summary>
    /// 允许作为属性名语义后缀的单个敏感词
    /// </summary>
    private static readonly HashSet<string> SensitiveSuffixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "secret",
        "token",
        "authorization",
        "credential",
        "cookie"
    };

    /// <summary>
    /// 对日志事件中的受控敏感标量属性执行脱敏
    /// </summary>
    /// <param name="logEvent">待处理的日志事件</param>
    /// <param name="propertyFactory">用于替换属性值的工厂</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var property in logEvent.Properties.ToArray())
        {
            if (!IsSensitive(property.Key) || property.Value is not ScalarValue scalar)
            {
                continue;
            }

            var masked = dataMasker.Mask(Convert.ToString(scalar.Value), SensitiveDataKind.Token);
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(property.Key, masked));
        }
    }

    /// <summary>
    /// 按属性名的单词边界判断是否属于受控敏感属性
    /// </summary>
    /// <param name="name">待判断的属性名</param>
    /// <returns>属于受控敏感属性时返回 <see langword="true"/></returns>
    private static bool IsSensitive(string name)
    {
        var compactName = new string(name.Where(char.IsLetterOrDigit).ToArray());
        if (CompactSensitiveNames.Contains(compactName))
        {
            return true;
        }

        var words = SplitWords(name);
        if (words.Count == 0)
        {
            return false;
        }

        if (SensitiveSuffixes.Contains(words[^1]))
        {
            return true;
        }

        return HasSuffix(words, "connection", "string")
            || HasSuffix(words, "api", "key")
            || HasSuffix(words, "private", "key")
            || HasSuffix(words, "authorization", "header")
            || HasSuffix(words, "cookie", "header");
    }

    /// <summary>
    /// 按分隔符、大小写转换和缩写边界拆分属性名
    /// </summary>
    /// <param name="name">待拆分的属性名</param>
    /// <returns>小写语义词列表</returns>
    private static IReadOnlyList<string> SplitWords(string name)
    {
        var words = new List<string>();
        var wordStart = -1;

        for (var index = 0; index < name.Length; index++)
        {
            if (!char.IsLetterOrDigit(name[index]))
            {
                AddWord(words, name, wordStart, index);
                wordStart = -1;
                continue;
            }

            if (wordStart < 0)
            {
                wordStart = index;
                continue;
            }

            if (!StartsNewWord(name, index))
            {
                continue;
            }

            AddWord(words, name, wordStart, index);
            wordStart = index;
        }

        AddWord(words, name, wordStart, name.Length);
        return words;
    }

    /// <summary>
    /// 判断当前位置是否开始新的大小写语义词
    /// </summary>
    /// <param name="name">完整属性名</param>
    /// <param name="index">当前字符位置</param>
    /// <returns>当前位置开始新词时返回 <see langword="true"/></returns>
    private static bool StartsNewWord(string name, int index)
    {
        if (!char.IsUpper(name[index]))
        {
            return false;
        }

        var previous = name[index - 1];
        if (char.IsLower(previous) || char.IsDigit(previous))
        {
            return true;
        }

        return char.IsUpper(previous)
            && index + 1 < name.Length
            && char.IsLower(name[index + 1]);
    }

    /// <summary>
    /// 把非空属性名片段加入语义词列表
    /// </summary>
    /// <param name="words">目标语义词列表</param>
    /// <param name="name">完整属性名</param>
    /// <param name="start">片段起始位置</param>
    /// <param name="end">片段结束位置</param>
    private static void AddWord(ICollection<string> words, string name, int start, int end)
    {
        if (start < 0 || end <= start)
        {
            return;
        }

        words.Add(name[start..end].ToLowerInvariant());
    }

    /// <summary>
    /// 判断语义词列表是否以指定双词组合结束
    /// </summary>
    /// <param name="words">属性名语义词列表</param>
    /// <param name="first">倒数第二个词</param>
    /// <param name="second">最后一个词</param>
    /// <returns>后缀匹配时返回 <see langword="true"/></returns>
    private static bool HasSuffix(IReadOnlyList<string> words, string first, string second)
    {
        return words.Count >= 2
            && string.Equals(words[^2], first, StringComparison.OrdinalIgnoreCase)
            && string.Equals(words[^1], second, StringComparison.OrdinalIgnoreCase);
    }
}
