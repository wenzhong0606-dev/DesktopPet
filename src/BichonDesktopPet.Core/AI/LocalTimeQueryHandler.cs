using System;
using BichonDesktopPet.Core.Services;

namespace BichonDesktopPet.Core.AI;

/// <summary>
/// Answers unambiguous local date/time questions from the operating-system clock,
/// without sending the message to a cloud model.
/// </summary>
public static class LocalTimeQueryHandler
{
    public static bool CanHandle(string? message) =>
        TryHandle(message, out _, DateTimeOffset.Now, TimeZoneInfo.Local);

    public static bool TryHandle(
        string? message,
        out string answer,
        DateTimeOffset? now = null,
        TimeZoneInfo? timeZone = null)
    {
        answer = string.Empty;
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var normalized = message.Trim().Replace("？", "?").Replace("！", "!");
        var asksTime = ContainsAny(normalized,
            "几点", "当前时间", "现在时间", "本地时间", "此刻时间", "时间是多少", "现在是什么时候", "现在是什么时间");
        var asksDate = ContainsAny(normalized,
            "今天几号", "今天是几号", "今天日期", "当前日期", "现在日期", "现在几号", "日期是多少");
        var asksWeekday = ContainsAny(normalized,
            "星期几", "周几", "今天星期", "今天周几");

        if (!asksTime && !asksDate && !asksWeekday)
        {
            return false;
        }

        var zone = timeZone ?? TimeZoneInfo.Local;
        var localNow = TimeZoneInfo.ConvertTime(now ?? DateTimeOffset.Now, zone);
        var day = SystemClockService.ToChineseDayOfWeek(localNow.DayOfWeek);
        var offset = SystemClockService.FormatOffset(zone.GetUtcOffset(localNow));

        if (asksWeekday && !asksTime && !asksDate)
        {
            answer = $"今天是{day}。";
            return true;
        }

        if (asksDate && !asksTime)
        {
            answer = $"今天是 {localNow:yyyy年M月d日}，{day}。";
            return true;
        }

        if (asksTime && !asksDate && !asksWeekday)
        {
            answer = $"现在是 {localNow:HH:mm:ss}（{zone.Id}，{offset}）。";
            return true;
        }

        answer = $"现在是 {localNow:yyyy年M月d日 HH:mm:ss}，{day}（{zone.Id}，{offset}）。";
        return true;
    }

    private static bool ContainsAny(string value, params string[] phrases)
    {
        foreach (var phrase in phrases)
        {
            if (value.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
