namespace BichonDesktopPet.Core.AI;

/// <summary>
/// Provider boundary for AI text generation. Companion identity, memory and
/// behavior code must depend on this contract rather than a vendor SDK.
/// </summary>
public interface IAiChatProvider
{
    string ProviderName { get; }

    string Model { get; }

    bool IsConfigured { get; }

    Task<AiChatResult> CompleteAsync(
        AiChatRequest request,
        CancellationToken cancellationToken = default);
}
