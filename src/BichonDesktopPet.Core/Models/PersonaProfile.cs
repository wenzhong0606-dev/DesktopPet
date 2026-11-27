namespace BichonDesktopPet.Core.Models;

/// <summary>
/// Identity and deterministic communication preferences for one avatar.
/// This is deliberately provider-neutral and contains no AI conversation memory.
/// </summary>
public sealed class PersonaProfile
{
    /// <summary>Optional alternative names users may use for this pet.</summary>
    public List<string> Nicknames { get; set; } = [];

    /// <summary>How the pet refers to itself in generated messages.</summary>
    public string SelfReference { get; set; } = "我";

    /// <summary>How the pet addresses its user.</summary>
    public string UserAddress { get; set; } = "你";

    /// <summary>Up to three free-form personality traits.</summary>
    public List<string> PersonalityTraits { get; set; } = [];

    /// <summary>cute, gentle, cool, tsundere, playful, or natural.</summary>
    public string SpeechStyle { get; set; } = "cute";

    /// <summary>short or medium.</summary>
    public string ResponseLength { get; set; } = "short";

    /// <summary>low, medium, or high.</summary>
    public string EmojiLevel { get; set; } = "medium";

    /// <summary>Optional phrases used for happy/excited reactions.</summary>
    public List<string> Catchphrases { get; set; } = [];
}
