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
    /// 无可靠单词边界时需要识别的敏感紧凑片段
    /// </summary>
    private static readonly string[] SensitiveCompactFragments =
    [
        "password",
        "secret",
        "token",
        "connectionstring",
        "apikey",
        "authorization",
        "credential",
        "privatekey",
        "cookie"
    ];

    /// <summary>
    /// 属性名任意语义位置都需要识别的单个敏感词
    /// </summary>
    private static readonly HashSet<string> SensitiveWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "secret",
        "token",
        "authorization",
        "credential",
        "cookie"
    };

    /// <summary>
    /// 属性名任意语义位置都需要识别的敏感短语
    /// </summary>
    private static readonly string[][] SensitivePhrases =
    [
        ["connection", "string"],
        ["api", "key"],
        ["private", "key"],
        ["authorization", "header"],
        ["cookie", "header"]
    ];

    /// <summary>
    /// 明确描述框架概念、规则或实现元数据的受控 benign 语义序列
    /// </summary>
    private static readonly string[][] BenignSequences =
    [
        ["cancellation", "token"],
        ["token", "bucket"],
        ["password", "policy"],
        ["authorization", "policy"],
        ["credential", "provider"],
        ["private", "key", "algorithm"],
        ["connection", "string", "builder"],
        ["cookie", "policy"],
        ["secretariat"],
        ["tokenization"],
        ["api", "keyboard"]
    ];

    /// <summary>
    /// 允许跟随受控 benign 序列的元数据尾词
    /// </summary>
    private static readonly HashSet<string> BenignMetadataTailWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "requested",
        "capacity",
        "name",
        "type",
        "count",
        "layout"
    };

    /// <summary>
    /// 受控 benign 语义序列对应的紧凑形式
    /// </summary>
    private static readonly string[] BenignCompactSequences =
        BenignSequences.Select(static sequence => string.Concat(sequence)).ToArray();

    /// <summary>
    /// 受控元数据尾词对应的紧凑形式
    /// </summary>
    private static readonly string[] BenignCompactMetadataTails = BenignMetadataTailWords.ToArray();

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
    /// 按紧凑片段和属性名语义边界判断是否属于受控敏感属性
    /// </summary>
    /// <param name="name">待判断的属性名</param>
    /// <returns>属于受控敏感属性时返回 <see langword="true"/></returns>
    private static bool IsSensitive(string name)
    {
        var compactName = new string(name.Where(char.IsLetterOrDigit).ToArray());
        var compactSearchLength = GetSensitiveCompactLength(compactName);
        if (ContainsSensitiveCompactFragment(compactName, compactSearchLength))
        {
            return true;
        }

        var words = SplitWords(name);
        if (words.Count == 0)
        {
            return false;
        }

        var searchWordCount = GetSensitiveWordCount(words);

        return words.Take(searchWordCount).Any(SensitiveWords.Contains)
            || SensitivePhrases.Any(phrase => ContainsSequence(words, searchWordCount, phrase));
    }

    /// <summary>
    /// 计算紧凑属性名中需要参与敏感片段扫描的前缀长度
    /// </summary>
    /// <param name="compactName">移除分隔符后的属性名</param>
    /// <returns>排除受控 benign 后缀后的扫描长度</returns>
    private static int GetSensitiveCompactLength(string compactName)
    {
        var searchLength = compactName.Length;
        foreach (var metadataTail in BenignCompactMetadataTails)
        {
            if (!EndsWith(compactName, searchLength, metadataTail))
            {
                continue;
            }

            searchLength -= metadataTail.Length;
            break;
        }

        foreach (var benignSequence in BenignCompactSequences)
        {
            if (EndsWith(compactName, searchLength, benignSequence))
            {
                return searchLength - benignSequence.Length;
            }
        }

        return searchLength;
    }

    /// <summary>
    /// 判断紧凑属性名的受控前缀是否包含敏感片段
    /// </summary>
    /// <param name="compactName">移除分隔符后的属性名</param>
    /// <param name="searchLength">参与扫描的前缀长度</param>
    /// <returns>扫描范围包含敏感片段时返回 <see langword="true"/></returns>
    private static bool ContainsSensitiveCompactFragment(string compactName, int searchLength)
    {
        return SensitiveCompactFragments.Any(
            fragment => compactName.IndexOf(fragment, 0, searchLength, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    /// <summary>
    /// 计算语义词列表中需要参与敏感语义扫描的前缀长度
    /// </summary>
    /// <param name="words">属性名语义词列表</param>
    /// <returns>排除受控 benign 后缀后的语义词数量</returns>
    private static int GetSensitiveWordCount(IReadOnlyList<string> words)
    {
        var searchWordCount = words.Count;
        if (searchWordCount > 0 && BenignMetadataTailWords.Contains(words[searchWordCount - 1]))
        {
            searchWordCount--;
        }

        foreach (var benignSequence in BenignSequences)
        {
            if (HasSuffix(words, searchWordCount, benignSequence))
            {
                return searchWordCount - benignSequence.Length;
            }
        }

        return searchWordCount;
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
    /// 判断指定范围的语义词列表是否以受控组合结束
    /// </summary>
    /// <param name="words">属性名语义词列表</param>
    /// <param name="wordCount">参与匹配的语义词数量</param>
    /// <param name="suffix">需要匹配的受控组合</param>
    /// <returns>后缀匹配时返回 <see langword="true"/></returns>
    private static bool HasSuffix(
        IReadOnlyList<string> words,
        int wordCount,
        IReadOnlyList<string> suffix)
    {
        if (wordCount < suffix.Count)
        {
            return false;
        }

        var start = wordCount - suffix.Count;
        for (var index = 0; index < suffix.Count; index++)
        {
            if (!string.Equals(words[start + index], suffix[index], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 判断指定范围的语义词列表任意位置是否包含敏感短语
    /// </summary>
    /// <param name="words">属性名语义词列表</param>
    /// <param name="wordCount">参与匹配的语义词数量</param>
    /// <param name="sequence">需要连续匹配的敏感短语</param>
    /// <returns>任意位置完整匹配短语时返回 <see langword="true"/></returns>
    private static bool ContainsSequence(
        IReadOnlyList<string> words,
        int wordCount,
        IReadOnlyList<string> sequence)
    {
        for (var start = 0; start <= wordCount - sequence.Count; start++)
        {
            var matches = true;
            for (var index = 0; index < sequence.Count; index++)
            {
                if (string.Equals(words[start + index], sequence[index], StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                matches = false;
                break;
            }

            if (matches)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断紧凑属性名的指定范围是否以受控文本结束
    /// </summary>
    /// <param name="compactName">移除分隔符后的属性名</param>
    /// <param name="searchLength">参与匹配的前缀长度</param>
    /// <param name="suffix">需要匹配的受控文本</param>
    /// <returns>指定范围以后缀结束时返回 <see langword="true"/></returns>
    private static bool EndsWith(string compactName, int searchLength, string suffix)
    {
        return searchLength >= suffix.Length
            && compactName.AsSpan(searchLength - suffix.Length, suffix.Length)
                .Equals(suffix, StringComparison.OrdinalIgnoreCase);
    }
}
