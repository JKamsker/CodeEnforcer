using System.Text.Json;

namespace CodeEnforcer;

internal static class JsonConfiguration
{
    public static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
