using System;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class ActivityRhythmEngineTests
{
    [TestMethod]
    public void Observe_RemindsAfterSixtyMinutesOfContinuousActivity()
    {
        var engine = new ActivityRhythmEngine();
        var start = new DateTimeOffset(2026, 11, 27, 9, 0, 0, TimeSpan.Zero);

        Assert.IsNull(engine.Observe(start, TimeSpan.Zero));
        Assert.IsNull(engine.Observe(start.AddMinutes(59), TimeSpan.Zero));
        var reminder = engine.Observe(start.AddMinutes(60), TimeSpan.Zero);

        Assert.IsNotNull(reminder);
        Assert.AreEqual(CareReminderType.TakeBreak, reminder!.Type);
    }

    [TestMethod]
    public void Observe_IdlePeriodResetsContinuousActivitySession()
    {
        var engine = new ActivityRhythmEngine();
        var start = new DateTimeOffset(2026, 11, 27, 9, 0, 0, TimeSpan.Zero);

        engine.Observe(start, TimeSpan.Zero);
        Assert.IsNull(engine.Observe(start.AddMinutes(59), TimeSpan.FromMinutes(6)));
        Assert.IsFalse(engine.IsCurrentlyActive);
        Assert.IsNull(engine.Observe(start.AddMinutes(65), TimeSpan.Zero));
        Assert.AreEqual(TimeSpan.Zero, engine.CurrentActiveDuration);
    }

    [TestMethod]
    public void Observe_UsesCooldownForLongContinuousSessions()
    {
        var engine = new ActivityRhythmEngine();
        var start = new DateTimeOffset(2026, 11, 27, 9, 0, 0, TimeSpan.Zero);

        Assert.IsNull(engine.Observe(start, TimeSpan.Zero));
        Assert.IsNotNull(engine.Observe(start.AddMinutes(60), TimeSpan.Zero));
        Assert.IsNull(engine.Observe(start.AddMinutes(100), TimeSpan.Zero));
        Assert.IsNotNull(engine.Observe(start.AddMinutes(150), TimeSpan.Zero));
    }
}
