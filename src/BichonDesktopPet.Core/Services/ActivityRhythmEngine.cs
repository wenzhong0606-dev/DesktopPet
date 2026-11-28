using System;

namespace BichonDesktopPet.Core.Services;

public enum CareReminderType
{
    TakeBreak,
    ColdWeather,
    RainExpected,
    HeatWarning
}

public sealed record CareReminder(CareReminderType Type, DateTimeOffset CreatedAt);

/// <summary>
/// Tracks only continuous active/idle durations. It never observes keyboard contents,
/// mouse coordinates, applications, URLs or window titles.
/// </summary>
public sealed class ActivityRhythmEngine
{
    public static readonly TimeSpan ActiveIdleThreshold = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan BreakReminderThreshold = TimeSpan.FromMinutes(60);
    public static readonly TimeSpan ReminderCooldown = TimeSpan.FromMinutes(90);

    private DateTimeOffset? _activeSessionStartedAt;
    private DateTimeOffset? _lastReminderAt;

    public TimeSpan CurrentActiveDuration { get; private set; }
    public bool IsCurrentlyActive { get; private set; }

    public CareReminder? Observe(DateTimeOffset now, TimeSpan idleDuration, bool acknowledge = true)
    {
        if (idleDuration < TimeSpan.Zero)
        {
            Reset();
            return null;
        }

        if (idleDuration >= ActiveIdleThreshold)
        {
            Reset();
            return null;
        }

        IsCurrentlyActive = true;
        if (_activeSessionStartedAt is null || now < _activeSessionStartedAt.Value)
        {
            _activeSessionStartedAt = now - idleDuration;
            _lastReminderAt = null;
        }

        CurrentActiveDuration = now - _activeSessionStartedAt.Value;
        if (CurrentActiveDuration < BreakReminderThreshold)
        {
            return null;
        }

        if (_lastReminderAt is not null &&
            now >= _lastReminderAt.Value &&
            now - _lastReminderAt.Value < ReminderCooldown)
        {
            return null;
        }

        if (acknowledge) MarkDelivered(now);
        return new CareReminder(CareReminderType.TakeBreak, now);
    }

    public void MarkDelivered(DateTimeOffset now) => _lastReminderAt = now;

    public void Reset()
    {
        _activeSessionStartedAt = null;
        _lastReminderAt = null;
        CurrentActiveDuration = TimeSpan.Zero;
        IsCurrentlyActive = false;
    }
}
