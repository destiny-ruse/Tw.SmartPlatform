using System.Globalization;
using Newtonsoft.Json;

namespace Tw.AspNetCore.Mvc.NewtonsoftJson;

/// <summary>
/// 在 Newtonsoft.Json 中将 long 标识序列化为字符串
/// </summary>
public sealed class LongIdJsonConverter : JsonConverter
{
    /// <summary>
    /// 判断指定类型是否由当前 JSON 转换器处理
    /// </summary>
    /// <param name="objectType">需要判断或创建的目标对象类型</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    public override bool CanConvert(Type objectType)
    {
        var type = Nullable.GetUnderlyingType(objectType) ?? objectType;
        return type == typeof(long);
    }

    /// <summary>
    /// 将长整型标识写入 JSON 输出
    /// </summary>
    /// <param name="writer">写入 JSON 内容的输出器</param>
    /// <param name="value">用于转换、回显或断言的输入值</param>
    /// <param name="serializer">当前 JSON 序列化器实例</param>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteValue(Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// 从 JSON 输入读取长整型标识
    /// </summary>
    /// <param name="reader">读取 JSON 内容的输入器</param>
    /// <param name="objectType">需要判断或创建的目标对象类型</param>
    /// <param name="existingValue">反序列化前已有的对象值</param>
    /// <param name="serializer">当前 JSON 序列化器实例</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
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
