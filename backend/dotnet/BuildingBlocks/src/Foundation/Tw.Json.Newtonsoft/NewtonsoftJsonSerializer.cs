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

    private sealed class Int64StringJsonConverter : JsonConverter<long>
    {
        public override void WriteJson(JsonWriter writer, long value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString(CultureInfo.InvariantCulture));
        }

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

    private sealed class NullableInt64StringJsonConverter : JsonConverter<long?>
    {
        public override void WriteJson(JsonWriter writer, long? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(value.Value.ToString(CultureInfo.InvariantCulture));
        }

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
