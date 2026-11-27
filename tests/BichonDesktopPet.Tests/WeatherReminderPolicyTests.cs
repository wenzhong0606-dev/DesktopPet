using System;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class WeatherReminderPolicyTests
{
    [TestMethod]
    public void Evaluate_ColdFrontSuggestsAddingClothes()
    {
        var now = new DateTimeOffset(2026, 11, 27, 8, 0, 0, TimeSpan.Zero);
        var snapshot = new WeatherSnapshot(now, 18, 17, 0, 1,
        [
            new WeatherHour(now.AddHours(2), 11, 9, 5, 0),
            new WeatherHour(now.AddHours(4), 10, 8, 10, 0)
        ]);

        var alert = new WeatherReminderPolicy().Evaluate(snapshot, now);

        Assert.IsNotNull(alert);
        Assert.AreEqual(CareReminderType.ColdWeather, alert!.Type);
    }

    [TestMethod]
    public void Evaluate_RainForecastSuggestsUmbrella()
    {
        var now = new DateTimeOffset(2026, 11, 27, 8, 0, 0, TimeSpan.Zero);
        var snapshot = new WeatherSnapshot(now, 22, 22, 10, 2,
        [
            new WeatherHour(now.AddHours(1), 22, 22, 75, 0.4)
        ]);

        var alert = new WeatherReminderPolicy().Evaluate(snapshot, now);

        Assert.IsNotNull(alert);
        Assert.AreEqual(CareReminderType.RainExpected, alert!.Type);
    }

    [TestMethod]
    public void Evaluate_StaleOrMissingForecastDoesNotRemind()
    {
        var now = new DateTimeOffset(2026, 11, 27, 8, 0, 0, TimeSpan.Zero);
        var stale = new WeatherSnapshot(now.AddHours(-3), 0, 0, 100, 65, []);
        var missing = new WeatherSnapshot(now, null, null, null, null, []);

        Assert.IsNull(new WeatherReminderPolicy().Evaluate(stale, now));
        Assert.IsNull(new WeatherReminderPolicy().Evaluate(missing, now));
    }
}
