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
    /// <summary>表示 Settings 字段</summary>
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

    /// <summary>表示 Int64StringJsonConverter 类型</summary>
    private sealed class Int64StringJsonConverter : JsonConverter<long>
    {
        /// <summary>执行 WriteJson 操作</summary>
        /// <param name="writer">writer 参数</param>
        /// <param name="value">value 参数</param>
        /// <param name="serializer">serializer 参数</param>
        public override void WriteJson(JsonWriter writer, long value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>执行 ReadJson 操作</summary>
        /// <param name="reader">reader 参数</param>
        /// <param name="objectType">objectType 参数</param>
        /// <param name="existingValue">existingValue 参数</param>
        /// <param name="hasExistingValue">hasExistingValue 参数</param>
        /// <param name="serializer">serializer 参数</param>
        /// <returns>ReadJson 的执行结果</returns>
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

    /// <summary>表示 NullableInt64StringJsonConverter 类型</summary>
    private sealed class NullableInt64StringJsonConverter : JsonConverter<long?>
    {
        /// <summary>执行 WriteJson 操作</summary>
        /// <param name="writer">writer 参数</param>
        /// <param name="value">value 参数</param>
        /// <param name="serializer">serializer 参数</param>
        public override void WriteJson(JsonWriter writer, long? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(value.Value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>执行 ReadJson 操作</summary>
        /// <param name="reader">reader 参数</param>
        /// <param name="objectType">objectType 参数</param>
        /// <param name="existingValue">existingValue 参数</param>
        /// <param name="hasExistingValue">hasExistingValue 参数</param>
        /// <param name="serializer">serializer 参数</param>
        /// <returns>ReadJson 的执行结果</returns>
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

    /// <summary>执行 ReadInt64 操作</summary>
    /// <param name="value">value 参数</param>
    /// <returns>ReadInt64 的执行结果</returns>
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
