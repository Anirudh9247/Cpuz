using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agent.Network.Json;

public static class AgentJsonSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };

    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, DefaultOptions);
    }

    public static T? Deserialize<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    public static JsonSerializerOptions GetOptions() => DefaultOptions;
}
