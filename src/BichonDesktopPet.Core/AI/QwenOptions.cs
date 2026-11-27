namespace BichonDesktopPet.Core.AI;

/// <summary>
/// Local Qwen configuration. ApiKey is held in memory only and must never be logged.
/// </summary>
public sealed class QwenOptions
{
    public const string DefaultEndpoint =
        "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";

    public const string DefaultModel = "qwen-plus";

    public string ApiKey { get; init; } = string.Empty;

    public string Endpoint { get; init; } = DefaultEndpoint;

    public string Model { get; init; } = DefaultModel;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
