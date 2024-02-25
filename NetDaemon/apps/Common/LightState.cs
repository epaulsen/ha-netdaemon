using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyNetDaemon.apps.Common;


public class LightState : IEquatable<LightState>
{
    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("brightness")]
    [JsonConverter(typeof(BrightnessConverter))]
    public int Brightness { get; set; }

    [JsonPropertyName("transition")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? Transition { get; set; }

    public bool Equals(LightState? other)
    {
        if (string.Compare(State, "OFF", StringComparison.InvariantCultureIgnoreCase) == 0 && string.Compare(other.State, "OFF", StringComparison.InvariantCultureIgnoreCase) == 0)
        {
            return true;
        }

        if (string.Compare(State, other.State, StringComparison.InvariantCultureIgnoreCase) != 0)
        {
            return false;
        }

        if (Math.Abs(Brightness - other.Brightness) > 5)
        {
            return false;
        }

        return true;
    }
}

public class BrightnessConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (int.TryParse(reader.GetString(), out var result))
            {
                return result;
            }
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }

        return 0;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
