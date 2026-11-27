using System;
using System.Linq;
using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.Core.Services;

/// <summary>
/// Converts actual forecast values into conservative clothing/rain/heat suggestions.
/// Missing or stale data never produces a reminder.
/// </summary>
public sealed class WeatherReminderPolicy
{
    public static readonly TimeSpan MaximumSnapshotAge = TimeSpan.FromHours(2);

    public WeatherCareAlert? Evaluate(WeatherSnapshot snapshot, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.RetrievedAtUtc > now || now - snapshot.RetrievedAtUtc > MaximumSnapshotAge)
        {
            return null;
        }

        var nextSixHours = snapshot.UpcomingHours
            .Where(hour => hour.Time >= now && hour.Time <= now.AddHours(6))
            .OrderBy(hour => hour.Time)
            .ToArray();
        var nextThreeHours = nextSixHours
            .Where(hour => hour.Time <= now.AddHours(3))
            .ToArray();

        var apparent = snapshot.CurrentApparentTemperatureC ?? snapshot.CurrentTemperatureC;
        var upcomingFeelsLike = nextSixHours
            .Select(hour => hour.ApparentTemperatureC ?? hour.TemperatureC)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();

        if ((apparent.HasValue && apparent.Value >= 32) ||
            upcomingFeelsLike.Any(value => value >= 35))
        {
            return new WeatherCareAlert(CareReminderType.HeatWarning, "体感温度较高，注意补水和防暑。", apparent);
        }

        var upcomingTemperatures = nextSixHours
            .Select(hour => hour.TemperatureC)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();
        var currentTemperature = snapshot.CurrentTemperatureC;
        var forecastMinimum = upcomingTemperatures.Length == 0 ? (double?)null : upcomingTemperatures.Min();
        var currentFeelsCold = apparent.HasValue && apparent.Value <= 8;
        var significantDrop = currentTemperature.HasValue &&
            forecastMinimum.HasValue &&
            currentTemperature.Value - forecastMinimum.Value >= 6 &&
            forecastMinimum.Value <= 14;

        if (currentFeelsCold || (forecastMinimum.HasValue && forecastMinimum.Value <= 8) || significantDrop)
        {
            return new WeatherCareAlert(CareReminderType.ColdWeather, "气温偏低或即将明显下降，出门记得添衣。", forecastMinimum);
        }

        var rainProbability = nextThreeHours
            .Select(hour => hour.PrecipitationProbability)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .DefaultIfEmpty(0)
            .Max();
        if (rainProbability >= 70)
        {
            return new WeatherCareAlert(CareReminderType.RainExpected, "未来几小时降雨概率较高，出门记得带伞。", rainProbability);
        }

        return null;
    }
}
