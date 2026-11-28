using System;
using System.Collections.Generic;
using System.Linq;

namespace BichonDesktopPet.Core.Services;

public sealed record CareContext(DateTimeOffset Now, bool ChatVisible, bool? Active,
    bool DoNotDisturb, bool Busy, bool ReduceFrequency, bool ReduceBreaks, bool Concise);

/// <summary>Local delivery gate shared by activity and weather. No inferred sleep schedule.</summary>
public sealed class ProactiveCarePolicy
{
    private DateTimeOffset? _lastDelivered;
    private readonly Queue<DateTimeOffset> _delivered = new();

    public void Restore(IEnumerable<DateTimeOffset>? history, DateTimeOffset now)
    {
        _delivered.Clear();
        _lastDelivered = null;
        foreach (var time in (history ?? Array.Empty<DateTimeOffset>()).Where(t => t <= now && t > now.AddHours(-24)).Distinct().OrderBy(t => t).TakeLast(4))
            MarkDelivered(time);
    }

    public DateTimeOffset[] Snapshot() => _delivered.ToArray();

    public bool CanDeliver(CareReminderType type, CareContext context)
    {
        if (context.ChatVisible || context.Active == false || context.DoNotDisturb || context.Busy)
            return false;
        if (type == CareReminderType.TakeBreak && (context.Active != true || context.ReduceBreaks))
            return false;
        var gap = context.ReduceFrequency ? TimeSpan.FromHours(3) : TimeSpan.FromMinutes(90);
        if (_lastDelivered is { } last && (context.Now < last || context.Now-last < gap))
            return false;
        return _delivered.Count(t => t > context.Now.AddHours(-24)) < (context.ReduceFrequency ? 2 : 4);
    }

    public void MarkDelivered(DateTimeOffset now)
    {
        while (_delivered.Count > 0 && _delivered.Peek() <= now.AddHours(-24)) _delivered.Dequeue();
        _delivered.Enqueue(now);
        _lastDelivered=now;
    }
}
