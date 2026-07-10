using System.Text.Json;
using Tw.Exceptions;

namespace Tw.Localization.Json;

/// <summary>
/// 将 JSON 字符串解析为 <see cref="JsonTextResource"/> 的静态工具类
/// </summary>
public static class JsonTextResourceParser
{
    /// <summary>
    /// 将给定的 JSON 字符串解析为 <see cref="JsonTextResource"/>
    /// </summary>
    /// <param name="resourceName">资源集合的名称，例如 "App"</param>
    /// <param name="sourcePath">JSON 来源路径（用于错误消息），例如 "app.zh-Hans.json"</param>
    /// <param name="json">待解析的 JSON 文本</param>
    /// <returns>解析成功后的 <see cref="JsonTextResource"/> 实例</returns>
    /// <exception cref="ArgumentNullException">当任意参数为 <see langword="null"/> 时抛出</exception>
    /// <exception cref="ArgumentException">当任意参数为空白字符串时抛出</exception>
    /// <exception cref="TwConfigurationException">当 JSON 格式无效或不符合规范时抛出</exception>
    public static JsonTextResource Parse(string resourceName, string sourcePath, string json)
    {
        var validatedResourceName = Check.NotNullOrWhiteSpace(resourceName);
        var validatedSourcePath = Check.NotNullOrWhiteSpace(sourcePath);
        var validatedJson = Check.NotNullOrWhiteSpace(json);

        try
        {
            using var document = JsonDocument.Parse(validatedJson);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new TwConfigurationException($"JSON 多语言资源根节点必须是对象：{validatedSourcePath}");
            }

            if (!root.TryGetProperty("culture", out var cultureElement) ||
                cultureElement.ValueKind != JsonValueKind.String)
            {
                throw new TwConfigurationException($"JSON 多语言资源缺少 culture 字符串：{validatedSourcePath}");
            }

            var cultureName = Check.NotNullOrWhiteSpace(cultureElement.GetString());

            if (!CultureFallback.IsValidCulture(cultureName))
            {
                throw new TwConfigurationException($"JSON 多语言资源 culture 无效：{validatedSourcePath}");
            }

            if (!root.TryGetProperty("texts", out var textsElement) ||
                textsElement.ValueKind != JsonValueKind.Object)
            {
                throw new TwConfigurationException($"JSON 多语言资源缺少 texts 对象：{validatedSourcePath}");
            }

            var texts = new Dictionary<string, string>(StringComparer.Ordinal);
            FlattenTexts(textsElement, prefix: string.Empty, texts, validatedSourcePath);

            return new JsonTextResource(validatedResourceName, cultureName, texts);
        }
        catch (JsonException exception)
        {
            throw new TwConfigurationException($"JSON 多语言资源格式错误：{validatedSourcePath}", exception);
        }
    }

    // 递归展开嵌套对象，使用 "__" 作为键分隔符；仅接受字符串叶子值
    /// <summary>
    /// 说明FlattenTexts在当前类型中的职责
    /// </summary>
    /// <param name="element">用于提供element</param>
    /// <param name="prefix">用于提供前缀</param>
    /// <param name="texts">用于提供texts</param>
    /// <param name="sourcePath">用于提供sourcePath</param>
    private static void FlattenTexts(
        JsonElement element,
        string prefix,
        IDictionary<string, string> texts,
        string sourcePath)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = string.IsNullOrWhiteSpace(prefix)
                ? property.Name
                : $"{prefix}__{property.Name}";

            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                FlattenTexts(property.Value, key, texts, sourcePath);
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.String)
            {
                // 若展开后的键与已有键重复（如嵌套路径与顶层键同名），后写入的值覆盖先写入的值（后写优先）；
                // 调用方应避免在同一资源文件内出现重复键
                texts[key] = property.Value.GetString() ?? string.Empty;
                continue;
            }

            throw new TwConfigurationException($"JSON 多语言资源 texts 叶子值必须是字符串：{sourcePath}");
        }
    }
}
