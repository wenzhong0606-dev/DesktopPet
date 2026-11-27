using System;
using System.Collections.Generic;
using BichonDesktopPet.Core.Services;

namespace BichonDesktopPet.Core.Models;

public sealed record WeatherHour(
    DateTimeOffset Time,
    double? TemperatureC,
    double? ApparentTemperatureC,
    double? PrecipitationProbability,
    double? PrecipitationMm);

public sealed record WeatherSnapshot(
    DateTimeOffset RetrievedAtUtc,
    double? CurrentTemperatureC,
    double? CurrentApparentTemperatureC,
    double? CurrentPrecipitationProbability,
    int? CurrentWeatherCode,
    IReadOnlyList<WeatherHour> UpcomingHours);

public sealed record WeatherCareAlert(
    CareReminderType Type,
    string Summary,
    double? RelevantValue = null);
