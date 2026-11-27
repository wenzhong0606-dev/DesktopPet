using System;

namespace BichonDesktopPet.Core.Services;

public sealed record SystemClockSnapshot(
    DateTimeOffset LocalNow,
    string TimeZoneId,
    string TimeZoneDisplayName,
    TimeSpan UtcOffset,
    string DayOfWeekChinese);

public sealed class SystemClockService
{
    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo _timeZone;

    public SystemClockService(TimeProvider? timeProvider = null, TimeZoneInfo? timeZone = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _timeZone = timeZone ?? TimeZoneInfo.Local;
    }

    public SystemClockSnapshot GetSnapshot()
    {
        var now = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), _timeZone);
        return new SystemClockSnapshot(
            now,
            _timeZone.Id,
            _timeZone.DisplayName,
            _timeZone.GetUtcOffset(now),
            ToChineseDayOfWeek(now.DayOfWeek));
    }

    public string BuildPromptContext()
    {
        var snapshot = GetSnapshot();
        return $"本机当前日期时间：{snapshot.LocalNow:yyyy年MM月dd日 HH:mm:ss}（{snapshot.DayOfWeekChinese}）；时区：{snapshot.TimeZoneId}；UTC 偏移：{FormatOffset(snapshot.UtcOffset)}。这是操作系统提供的实时本地时间，回答时间/日期问题时必须以此为准，不得猜测。";
    }

    public static string FormatOffset(TimeSpan offset)
    {
        var absolute = offset.Duration();
        return $"UTC{(offset < TimeSpan.Zero ? "-" : "+")}{absolute.Hours:00}:{absolute.Minutes:00}";
    }

    public static string ToChineseDayOfWeek(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "星期一",
        DayOfWeek.Tuesday => "星期二",
        DayOfWeek.Wednesday => "星期三",
        DayOfWeek.Thursday => "星期四",
        DayOfWeek.Friday => "星期五",
        DayOfWeek.Saturday => "星期六",
        _ => "星期日"
    };
}
