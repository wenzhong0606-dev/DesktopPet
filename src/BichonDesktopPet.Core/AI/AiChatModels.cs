namespace BichonDesktopPet.Core.AI;

/// <summary>
/// A provider-neutral chat message. Version one intentionally supports only
/// system, user and assistant roles; tools and arbitrary function execution are out of scope.
/// </summary>
public sealed record AiChatMessage(string Role, string Content);

public sealed class AiChatRequest
{
    public IReadOnlyList<AiChatMessage> Messages { get; init; } = Array.Empty<AiChatMessage>();

    public int MaxTokens { get; init; } = 512;

    public double? Temperature { get; init; }
}

public sealed class AiChatResult
{
    public string Content { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public int? PromptTokens { get; init; }

    public int? CompletionTokens { get; init; }

    public int? TotalTokens { get; init; }
}

public sealed class AiProviderNotConfiguredException : InvalidOperationException
{
    public AiProviderNotConfiguredException(string providerName)
        : base($"{providerName} is not configured. Add the API key locally before using AI features.")
    {
    }
}

public sealed class AiProviderException : Exception
{
    public AiProviderException(string message, int? statusCode = null, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public int? StatusCode { get; }
}
