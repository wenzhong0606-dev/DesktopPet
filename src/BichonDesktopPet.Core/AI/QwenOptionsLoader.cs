using System.Text.Json;

namespace BichonDesktopPet.Core.AI;

/// <summary>
/// Loads Qwen settings from %LOCALAPPDATA%\BichonDesktopPet\appsettings.Local.json.
/// Environment variables Qwen__ApiKey, Qwen__Endpoint and Qwen__Model override
/// local-file values. Secrets are never written by this class.
/// </summary>
public static class QwenOptionsLoader
{
    public const string LocalFileName = "appsettings.Local.json";

    public static QwenOptions Load(
        string userDataDirectory,
        Func<string, string?>? environmentVariableReader = null)
    {
        if (string.IsNullOrWhiteSpace(userDataDirectory))
        {
            throw new ArgumentException("A user data directory is required.", nameof(userDataDirectory));
        }

        environmentVariableReader ??= Environment.GetEnvironmentVariable;

        var apiKey = string.Empty;
        var endpoint = QwenOptions.DefaultEndpoint;
        var model = QwenOptions.DefaultModel;
        var localPath = Path.Combine(userDataDirectory, LocalFileName);

        if (File.Exists(localPath))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(localPath));
            if (document.RootElement.TryGetProperty("Qwen", out var qwen) &&
                qwen.ValueKind == JsonValueKind.Object)
            {
                apiKey = ReadString(qwen, "ApiKey") ?? apiKey;
                endpoint = ReadString(qwen, "Endpoint") ?? endpoint;
                model = ReadString(qwen, "Model") ?? model;
            }
        }

        apiKey = ReadNonEmpty(environmentVariableReader("Qwen__ApiKey")) ?? apiKey;
        endpoint = ReadNonEmpty(environmentVariableReader("Qwen__Endpoint")) ?? endpoint;
        model = ReadNonEmpty(environmentVariableReader("Qwen__Model")) ?? model;

        return new QwenOptions
        {
            ApiKey = apiKey.Trim(),
            Endpoint = endpoint.Trim(),
            Model = model.Trim()
        };
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return ReadNonEmpty(value.GetString());
    }

    private static string? ReadNonEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
