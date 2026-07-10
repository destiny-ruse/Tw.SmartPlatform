using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Tw.Json.Abstractions;

namespace Tw.Json.Newtonsoft;

/// <summary>
/// 基于 Newtonsoft.Json 的 JSON 序列化器实现
/// </summary>
public sealed class NewtonsoftJsonSerializer : IJsonSerializer
{
    /// <summary>
    /// 保存当前类型处理流程依赖的设置
    /// </summary>
    private static readonly JsonSerializerSettings Settings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        ReferenceLoopHandling = ReferenceLoopHandling.Error,
        TypeNameHandling = TypeNameHandling.None,
        Converters =
        {
            new Int64StringJsonConverter(),
            new NullableInt64StringJsonConverter()
        }
    };

    /// <inheritdoc />
    public string Serialize<T>(T value)
    {
        return JsonConvert.SerializeObject(value, Settings);
    }

    /// <inheritdoc />
    public T? Deserialize<T>(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonConvert.DeserializeObject<T>(json, Settings);
    }

    /// <summary>
    /// 在 Newtonsoft.Json 中将 long 值序列化为字符串
    /// </summary>
    private sealed class Int64StringJsonConverter : JsonConverter<long>
    {
        /// <summary>
        /// 将长整型标识写入 JSON 输出
        /// </summary>
        /// <param name="writer">写入 JSON 内容的输出器</param>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <param name="serializer">当前 JSON 序列化器实例</param>
        public override void WriteJson(JsonWriter writer, long value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 从 JSON 输入读取长整型标识
        /// </summary>
        /// <param name="reader">读取 JSON 内容的输入器</param>
        /// <param name="objectType">需要判断或创建的目标对象类型</param>
        /// <param name="existingValue">反序列化前已有的对象值</param>
        /// <param name="hasExistingValue">用于提供存在Existing值</param>
        /// <param name="serializer">当前 JSON 序列化器实例</param>
        /// <returns>方法完成后返回给调用方的结果对象</returns>
        public override long ReadJson(
            JsonReader reader,
            Type objectType,
            long existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                throw new JsonSerializationException("long 值不能为 null");
            }

            return ReadInt64(reader.Value);
        }
    }

    /// <summary>
    /// 在 Newtonsoft.Json 中将可空 long 值序列化为字符串
    /// </summary>
    private sealed class NullableInt64StringJsonConverter : JsonConverter<long?>
    {
        /// <summary>
        /// 将长整型标识写入 JSON 输出
        /// </summary>
        /// <param name="writer">写入 JSON 内容的输出器</param>
        /// <param name="value">用于转换、回显或断言的输入值</param>
        /// <param name="serializer">当前 JSON 序列化器实例</param>
        public override void WriteJson(JsonWriter writer, long? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(value.Value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 从 JSON 输入读取长整型标识
        /// </summary>
        /// <param name="reader">读取 JSON 内容的输入器</param>
        /// <param name="objectType">需要判断或创建的目标对象类型</param>
        /// <param name="existingValue">反序列化前已有的对象值</param>
        /// <param name="hasExistingValue">用于提供存在Existing值</param>
        /// <param name="serializer">当前 JSON 序列化器实例</param>
        /// <returns>方法完成后返回给调用方的结果对象</returns>
        public override long? ReadJson(
            JsonReader reader,
            Type objectType,
            long? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            return reader.TokenType == JsonToken.Null ? null : ReadInt64(reader.Value);
        }
    }

    /// <summary>
    /// 读取nt64内容
    /// </summary>
    /// <param name="value">用于转换、回显或断言的输入值</param>
    /// <returns>方法完成后返回给调用方的结果对象</returns>
    private static long ReadInt64(object? value)
    {
        if (value is long longValue)
        {
            return longValue;
        }

        var text = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        throw new JsonSerializationException("JSON long 值格式无效");
    }
}
