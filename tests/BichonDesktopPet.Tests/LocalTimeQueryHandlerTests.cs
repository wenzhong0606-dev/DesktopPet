using System;
using BichonDesktopPet.Core.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class LocalTimeQueryHandlerTests
{
    private static readonly TimeZoneInfo TestZone = TimeZoneInfo.CreateCustomTimeZone(
        "Test/UTC+08", TimeSpan.FromHours(8), "Test UTC+08", "Test UTC+08");

    private static readonly DateTimeOffset FixedNow =
        new(2026, 11, 27, 13, 16, 5, TimeSpan.Zero);

    [TestMethod]
    public void TryHandle_CurrentTime_ReturnsProvidedLocalTimeAndTimezone()
    {
        Assert.IsTrue(LocalTimeQueryHandler.TryHandle("现在几点？", out var answer, FixedNow, TestZone));
        StringAssert.Contains(answer, "21:16:05");
        StringAssert.Contains(answer, "Test/UTC+08");
        StringAssert.Contains(answer, "UTC+08:00");
    }

    [TestMethod]
    public void TryHandle_DateAndWeekday_ReturnsLocalDate()
    {
        Assert.IsTrue(LocalTimeQueryHandler.TryHandle("今天星期几？", out var weekday, FixedNow, TestZone));
        StringAssert.Contains(weekday, "星期五");
        Assert.IsTrue(LocalTimeQueryHandler.TryHandle("今天几号", out var date, FixedNow, TestZone));
        StringAssert.Contains(date, "2026年11月27日");
    }

    [TestMethod]
    public void TryHandle_UnrelatedQuestion_DoesNotIntercept()
    {
        Assert.IsFalse(LocalTimeQueryHandler.TryHandle("你觉得我几点睡觉比较好？", out _));
        Assert.IsFalse(LocalTimeQueryHandler.TryHandle("帮我解释一下时间复杂度", out _));
    }
}
