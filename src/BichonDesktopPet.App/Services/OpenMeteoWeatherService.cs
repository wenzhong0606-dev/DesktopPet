using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.App.Services;

/// <summary>
/// Open-Meteo city geocoding and forecast adapter. Weather requests are made only
/// after the user enables weather reminders.
/// </summary>
public sealed class OpenMeteoWeatherService
{
    private readonly HttpClient _httpClient;

    public OpenMeteoWeatherService(HttpClient httpClient) =>
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<CareLocation?> ResolveCityAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var url = "https://geocoding-api.open-meteo.com/v1/search?name=" +
            Uri.EscapeDataString(query.Trim()) + "&count=1&language=zh";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        if (!root.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array ||
            results.GetArrayLength() == 0)
        {
            return null;
        }

        var result = results[0];
        var city = ReadString(result, "name");
        var country = ReadString(result, "country");
        var latitude = ReadDouble(result, "latitude");
        var longitude = ReadDouble(result, "longitude");
        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(country) ||
            latitude is null || longitude is null)
        {
            return null;
        }

        var admin1 = ReadString(result, "admin1");
        var displayName = string.IsNullOrWhiteSpace(admin1)
            ? $"{city}, {country}"
            : $"{city}, {admin1}, {country}";

        return new CareLocation(
            displayName,
            city,
            country,
            latitude.Value,
            longitude.Value,
            ReadString(result, "timezone") ?? "auto",
            "manual",
            DateTimeOffset.UtcNow);
    }

    public async Task<WeatherSnapshot> GetForecastAsync(
        CareLocation location,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);
        var url = FormattableString.Invariant(
            $"https://api.open-meteo.com/v1/forecast?latitude={location.Latitude:F5}&longitude={location.Longitude:F5}&current=temperature_2m,apparent_temperature,precipitation,weather_code&hourly=temperature_2m,apparent_temperature,precipitation_probability,precipitation&forecast_hours=12&timezone=auto");

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var current = root.GetProperty("current");
        var hourly = root.GetProperty("hourly");

        var utcOffsetSeconds = root.TryGetProperty("utc_offset_seconds", out var offsetElement) &&
            offsetElement.ValueKind == JsonValueKind.Number && offsetElement.TryGetInt32(out var offsetValue)
                ? offsetValue
                : 0;
        var times = ReadDateTimeArray(hourly, "time", TimeSpan.FromSeconds(utcOffsetSeconds));
        var temperatures = ReadDoubleArray(hourly, "temperature_2m");
        var apparentTemperatures = ReadDoubleArray(hourly, "apparent_temperature");
        var rainProbabilities = ReadDoubleArray(hourly, "precipitation_probability");
        var precipitation = ReadDoubleArray(hourly, "precipitation");

        var hours = new List<WeatherHour>();
        for (var index = 0; index < times.Count; index++)
        {
            hours.Add(new WeatherHour(
                times[index],
                At(temperatures, index),
                At(apparentTemperatures, index),
                At(rainProbabilities, index),
                At(precipitation, index)));
        }

        return new WeatherSnapshot(
            DateTimeOffset.UtcNow,
            ReadDouble(current, "temperature_2m"),
            ReadDouble(current, "apparent_temperature"),
            null,
            ReadInt(current, "weather_code"),
            hours);
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static double? ReadDouble(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number &&
        value.TryGetDouble(out var number)
            ? number
            : null;

    private static int? ReadInt(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number &&
        value.TryGetInt32(out var number)
            ? number
            : null;

    private static List<DateTimeOffset> ReadDateTimeArray(JsonElement element, string property, TimeSpan utcOffset)
    {
        if (!element.TryGetProperty(property, out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return values.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.String)
            .Select(value => ParseForecastTime(value.GetString()!, utcOffset))
            .ToList();
    }

    private static DateTimeOffset ParseForecastTime(string value, TimeSpan utcOffset)
    {
        var timeSeparator = value.IndexOf('T');
        var hasExplicitOffset = value.EndsWith("Z", StringComparison.OrdinalIgnoreCase) ||
            (timeSeparator >= 0 &&
             (value.LastIndexOf('+') > timeSeparator || value.LastIndexOf('-') > timeSeparator));

        if (hasExplicitOffset)
        {
            return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.None);
        }

        var localWallClock = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.None);
        return new DateTimeOffset(DateTime.SpecifyKind(localWallClock, DateTimeKind.Unspecified), utcOffset);
    }

    private static List<double?> ReadDoubleArray(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return values.EnumerateArray()
            .Select(value => value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number)
                ? (double?)number
                : null)
            .ToList();
    }

    private static double? At(IReadOnlyList<double?> values, int index) =>
        index >= 0 && index < values.Count ? values[index] : null;
}
