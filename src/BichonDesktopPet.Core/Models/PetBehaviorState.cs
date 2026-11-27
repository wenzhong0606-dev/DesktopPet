namespace BichonDesktopPet.Core.Models;

/// <summary>
/// High-level companion states. These describe intent, not a particular image or rig.
/// </summary>
public enum PetBehaviorState
{
    Idle,
    Greeting,
    Happy,
    Excited,
    Sleepy
}

/// <summary>
/// A presentation effect that can be rendered by the current image-based WPF host.
/// These are deliberately whole-image effects, not skeletal animation claims.
/// </summary>
public enum PetVisualEffect
{
    IdleBreathing,
    GreetingBounce,
    HappyBounce,
    ExcitedWiggle,
    SleepySway
}
