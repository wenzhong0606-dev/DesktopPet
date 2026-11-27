using System;
using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.Core.Services;

/// <summary>
/// A deterministic, platform-independent state machine for the desktop companion.
/// Time is supplied by the caller so transitions can be tested without timers.
/// </summary>
public sealed class PetBehaviorEngine
{
    public static readonly TimeSpan SleepAfterInactivity = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan RapidInteractionWindow = TimeSpan.FromSeconds(4);
    public static readonly TimeSpan GreetingDuration = TimeSpan.FromMilliseconds(2600);
    public static readonly TimeSpan HappyDuration = TimeSpan.FromMilliseconds(1700);
    public static readonly TimeSpan ExcitedDuration = TimeSpan.FromMilliseconds(2400);

    private bool _hasStarted;
    private DateTimeOffset _lastInteractionAt;
    private DateTimeOffset _interactionWindowStartedAt;
    private DateTimeOffset? _stateExpiresAt;
    private int _recentInteractionCount;

    public PetBehaviorState CurrentState { get; private set; } = PetBehaviorState.Idle;

    public DateTimeOffset? LastInteractionAt => _hasStarted ? _lastInteractionAt : null;

    public PetBehaviorState Start(DateTimeOffset now)
    {
        _hasStarted = true;
        _lastInteractionAt = now;
        _interactionWindowStartedAt = now;
        _recentInteractionCount = 0;
        SetState(PetBehaviorState.Greeting, now, GreetingDuration);
        return CurrentState;
    }

    public PetBehaviorState OnPetClicked(DateTimeOffset now)
    {
        RecordInteraction(now);

        var nextState = _recentInteractionCount >= 3
            ? PetBehaviorState.Excited
            : PetBehaviorState.Happy;

        SetState(
            nextState,
            now,
            nextState == PetBehaviorState.Excited ? ExcitedDuration : HappyDuration);

        return CurrentState;
    }

    public PetBehaviorState OnGiftOpened(DateTimeOffset now)
    {
        RecordInteraction(now);
        SetState(PetBehaviorState.Excited, now, ExcitedDuration);
        return CurrentState;
    }

    public PetBehaviorState OnWake(DateTimeOffset now)
    {
        RecordInteraction(now, resetInteractionBurst: true);
        SetState(PetBehaviorState.Greeting, now, GreetingDuration);
        return CurrentState;
    }

    /// <summary>
    /// Advances transient states and inactivity. Call from a host-owned timer.
    /// </summary>
    public PetBehaviorState Advance(DateTimeOffset now)
    {
        if (!_hasStarted)
        {
            return Start(now);
        }

        if (CurrentState == PetBehaviorState.Sleepy)
        {
            return CurrentState;
        }

        if (_stateExpiresAt is { } expiresAt && now >= expiresAt)
        {
            _stateExpiresAt = null;

            if (IsInactive(now))
            {
                SetState(PetBehaviorState.Sleepy, now, duration: null);
                return CurrentState;
            }

            SetState(PetBehaviorState.Idle, now, duration: null);
        }

        if (CurrentState == PetBehaviorState.Idle && IsInactive(now))
        {
            SetState(PetBehaviorState.Sleepy, now, duration: null);
        }

        return CurrentState;
    }

    private void RecordInteraction(DateTimeOffset now, bool resetInteractionBurst = false)
    {
        if (!_hasStarted)
        {
            _hasStarted = true;
            _recentInteractionCount = 0;
        }

        if (resetInteractionBurst ||
            _recentInteractionCount == 0 ||
            now < _interactionWindowStartedAt ||
            now - _interactionWindowStartedAt > RapidInteractionWindow)
        {
            _interactionWindowStartedAt = now;
            _recentInteractionCount = 1;
        }
        else
        {
            _recentInteractionCount++;
        }

        _lastInteractionAt = now;
    }

    private bool IsInactive(DateTimeOffset now) =>
        now >= _lastInteractionAt &&
        now - _lastInteractionAt >= SleepAfterInactivity;

    private void SetState(PetBehaviorState state, DateTimeOffset now, TimeSpan? duration)
    {
        CurrentState = state;
        _stateExpiresAt = duration is null ? null : now + duration.Value;
    }
}
