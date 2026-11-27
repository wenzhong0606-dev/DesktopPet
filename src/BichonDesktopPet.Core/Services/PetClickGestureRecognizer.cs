using System;

namespace BichonDesktopPet.Core.Services;

public readonly record struct PetClickGestureResult(
    int CompletedSingleClicks,
    bool IsDoubleClick,
    bool HasPendingSingleClick);

/// <summary>
/// Delays single-click recognition until the system double-click window has elapsed.
/// This keeps the first half of a double-click from being persisted as a single click.
/// </summary>
public sealed class PetClickGestureRecognizer
{
    private readonly TimeSpan _doubleClickInterval;
    private readonly double _doubleClickWidth;
    private readonly double _doubleClickHeight;
    private DateTimeOffset? _pendingClickAt;
    private double _pendingClickX;
    private double _pendingClickY;

    public PetClickGestureRecognizer(
        TimeSpan doubleClickInterval,
        double doubleClickWidth,
        double doubleClickHeight)
    {
        if (doubleClickInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(doubleClickInterval));
        }

        if (!double.IsFinite(doubleClickWidth) || doubleClickWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(doubleClickWidth));
        }

        if (!double.IsFinite(doubleClickHeight) || doubleClickHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(doubleClickHeight));
        }

        _doubleClickInterval = doubleClickInterval;
        _doubleClickWidth = doubleClickWidth;
        _doubleClickHeight = doubleClickHeight;
    }

    public PetClickGestureResult RegisterClick(DateTimeOffset timestamp, double x, double y)
    {
        if (_pendingClickAt is null)
        {
            StartPendingClick(timestamp, x, y);
            return new PetClickGestureResult(0, false, true);
        }

        var elapsed = timestamp - _pendingClickAt.Value;
        var isWithinTime = elapsed >= TimeSpan.Zero && elapsed <= _doubleClickInterval;
        var isWithinPosition =
            Math.Abs(x - _pendingClickX) <= _doubleClickWidth &&
            Math.Abs(y - _pendingClickY) <= _doubleClickHeight;

        if (isWithinTime && isWithinPosition)
        {
            _pendingClickAt = null;
            return new PetClickGestureResult(0, true, false);
        }

        // The previous click can no longer be part of a double-click. Complete it
        // as a single click and begin a fresh recognition window for this click.
        StartPendingClick(timestamp, x, y);
        return new PetClickGestureResult(1, false, true);
    }

    public bool TryCompleteSingleClick(DateTimeOffset now)
    {
        if (_pendingClickAt is null || now - _pendingClickAt.Value < _doubleClickInterval)
        {
            return false;
        }

        _pendingClickAt = null;
        return true;
    }

    public bool FlushPendingSingleClick()
    {
        if (_pendingClickAt is null)
        {
            return false;
        }

        _pendingClickAt = null;
        return true;
    }

    private void StartPendingClick(DateTimeOffset timestamp, double x, double y)
    {
        _pendingClickAt = timestamp;
        _pendingClickX = x;
        _pendingClickY = y;
    }
}
