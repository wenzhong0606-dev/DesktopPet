using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.Core.Services;

/// <summary>
/// Resolves default messages and image-host effects for each behavior state.
/// Species-specific wording is supported without coupling the state machine to an avatar.
/// </summary>
public sealed class PetBehaviorPresentationResolver
{
    public PetBehaviorPresentation Resolve(PetBehaviorState state, string? species)
    {
        var normalizedSpecies = string.IsNullOrWhiteSpace(species)
            ? "custom"
            : species.Trim().ToLowerInvariant();

        return new PetBehaviorPresentation(
            state,
            ResolveVisualEffect(state),
            ResolveMessage(state, normalizedSpecies));
    }

    private static PetVisualEffect ResolveVisualEffect(PetBehaviorState state) => state switch
    {
        PetBehaviorState.Greeting => PetVisualEffect.GreetingBounce,
        PetBehaviorState.Happy => PetVisualEffect.HappyBounce,
        PetBehaviorState.Excited => PetVisualEffect.ExcitedWiggle,
        PetBehaviorState.Sleepy => PetVisualEffect.SleepySway,
        _ => PetVisualEffect.IdleBreathing
    };

    private static string ResolveMessage(PetBehaviorState state, string species) =>
        (state, species) switch
        {
            (PetBehaviorState.Idle, "dog") => "汪～我在这里",
            (PetBehaviorState.Idle, "cat") => "喵～我在这里",
            (PetBehaviorState.Idle, _) => "我在这里陪着你",

            (PetBehaviorState.Greeting, "dog") => "汪！欢迎回来！",
            (PetBehaviorState.Greeting, "cat") => "喵！你回来啦",
            (PetBehaviorState.Greeting, _) => "欢迎回来！",

            (PetBehaviorState.Happy, "dog") => "嘿嘿，摸摸我！",
            (PetBehaviorState.Happy, "cat") => "呼噜呼噜～",
            (PetBehaviorState.Happy, "rabbit") => "蹦蹦！好开心",
            (PetBehaviorState.Happy, _) => "嘿嘿，好开心！",

            (PetBehaviorState.Excited, "dog") => "汪汪！好开心！",
            (PetBehaviorState.Excited, "cat") => "喵！好开心！",
            (PetBehaviorState.Excited, "rabbit") => "太开心啦！",
            (PetBehaviorState.Excited, _) => "耶！太棒啦！",

            (PetBehaviorState.Sleepy, "cat") => "我先打个盹...",
            (PetBehaviorState.Sleepy, _) => "我有点困啦...",

            _ => "我在这里陪着你"
        };
}
