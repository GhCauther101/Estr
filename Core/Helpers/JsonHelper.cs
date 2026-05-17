using System.Text.Json;

namespace Core.Helpers;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        WriteIndented = false, // compact by default
        PropertyNameCaseInsensitive = true
    };

    // ================================
    // ENCODE (object → JSON string)
    // ================================
    public static string Encode<T>(T obj, bool pretty = false)
    {
        var options = pretty
            ? new JsonSerializerOptions { WriteIndented = true }
            : Options;

        return JsonSerializer.Serialize(obj, options);
    }

    // ================================
    // DECODE (JSON → Dictionary<string, string>)
    // ================================
    public static IDictionary<string, string> DecodeToStringDictionary(string json)
    {
        var result = new Dictionary<string, string>();

        using var doc = JsonDocument.Parse(json);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            result[prop.Name] = ConvertJsonElementToString(prop.Value);
        }

        return result;
    }

    // ================================
    // Helper: Convert JsonElement → string
    // ================================
    private static string ConvertJsonElementToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString()!,
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null!,
            _ => element.ToString()
        };
    }

    // ================================
    // DECODE (JSON string → Dictionary<string, object>)
    // ================================
    public static IDictionary<string, object?> DecodeToObjectDictionary(string json)
    {
        var result = new Dictionary<string, object?>();

        using var doc = JsonDocument.Parse(json);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            result[prop.Name] = ConvertJsonElementToObject(prop.Value);
        }

        return result;
    }

    private static object? ConvertJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }
}