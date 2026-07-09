using System.Globalization;
using Newtonsoft.Json;

namespace Tw.AspNetCore.Mvc.NewtonsoftJson;

/// <summary>表示 LongIdJsonConverter 类型</summary>
public sealed class LongIdJsonConverter : JsonConverter
{
    /// <summary>执行 CanConvert 操作</summary>
    /// <param name="objectType">objectType 参数</param>
    /// <returns>CanConvert 的执行结果</returns>
    public override bool CanConvert(Type objectType)
    {
        var type = Nullable.GetUnderlyingType(objectType) ?? objectType;
        return type == typeof(long);
    }

    /// <summary>执行 WriteJson 操作</summary>
    /// <param name="writer">writer 参数</param>
    /// <param name="value">value 参数</param>
    /// <param name="serializer">serializer 参数</param>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteValue(Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>执行 ReadJson 操作</summary>
    /// <param name="reader">reader 参数</param>
    /// <param name="objectType">objectType 参数</param>
    /// <param name="existingValue">existingValue 参数</param>
    /// <param name="serializer">serializer 参数</param>
    /// <returns>ReadJson 的执行结果</returns>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return Nullable.GetUnderlyingType(objectType) is not null ? null : 0L;
        }

        var text = Convert.ToString(reader.Value, CultureInfo.InvariantCulture);
        if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            throw new JsonSerializationException("Long id must be a decimal string.");
        }

        return value;
    }
}
