using System.Globalization;
using Newtonsoft.Json;

namespace Tw.AspNetCore.Mvc.NewtonsoftJson;

public sealed class LongIdJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        var type = Nullable.GetUnderlyingType(objectType) ?? objectType;
        return type == typeof(long);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteValue(Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
    }

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
