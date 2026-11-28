using System;
using System.Collections.Generic;

namespace BichonDesktopPet.Core.Services;

/// <summary>Finite explicit ordinary states and intentions. No inferred health state,
/// calendar event, reminder or completed activity. Durations are rolling UTC TTLs.</summary>
public static class ShortTermMemoryExtractor
{
    private static readonly Dictionary<string, string> States = new(StringComparer.Ordinal)
    {
        ["我今天有点累"]="用户在消息中表示今天有点累",
        ["我今天很累"]="用户在消息中表示今天很累",
        ["我今天很忙"]="用户在消息中表示今天很忙",
        ["我今天有点忙"]="用户在消息中表示今天有点忙",
        ["我今天不忙"]="用户在消息中表示今天不忙",
        ["我今天不累"]="用户在消息中表示今天不累"
    };
    private static readonly HashSet<string> Activities = new(StringComparer.Ordinal)
        { "跑步", "散步", "看书", "游泳", "骑行", "画画", "学习" };

    public static MemoryCandidate? Extract(string? text)
    {
        var value=(text??"").Trim().TrimEnd('。','！','!');
        if(States.TryGetValue(value,out var state))
            return new MemoryCandidate(value.Contains("累")?"状态·疲劳自述":"状态·忙碌自述",state,"临时状态");
        foreach(var prefix in new[]{"我明天准备去","我明天打算去","我明天准备","我明天打算"})
        {
            if(!value.StartsWith(prefix,StringComparison.Ordinal))continue;
            var activity=value[prefix.Length..];
            if(Activities.Contains(activity))
                return new MemoryCandidate("计划·"+activity,$"用户在消息中表示明天打算{activity}；未确认执行。","近期计划");
        }
        return null;
    }

    public static TimeSpan? Lifetime(string category) => category switch
    {
        "临时状态" => TimeSpan.FromHours(8),
        "近期计划" => TimeSpan.FromHours(36),
        _ => null
    };
}
