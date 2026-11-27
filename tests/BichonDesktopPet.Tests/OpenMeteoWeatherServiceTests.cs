using System;
using System.Net.Http;
using System.Threading.Tasks;
using BichonDesktopPet.App.Services;
using BichonDesktopPet.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class OpenMeteoWeatherServiceTests
{
    [TestMethod]
    public async Task ResolveCityAsync_EncodesQueryAndReturnsTimezoneCoordinates()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(
            """
            {
              "results": [
                {
                  "name": "Los Angeles",
                  "admin1": "California",
                  "country": "United States",
                  "latitude": 34.0522,
                  "longitude": -118.2437,
                  "timezone": "America/Los_Angeles"
                }
              ]
            }
            """));
        using var client = new HttpClient(handler);

        var result = await new OpenMeteoWeatherService(client)
            .ResolveCityAsync("Los Angeles, United States");

        Assert.IsNotNull(result);
        Assert.AreEqual("Los Angeles", result!.City);
        Assert.AreEqual("Los Angeles, California, United States", result.DisplayName);
        Assert.AreEqual(34.0522, result.Latitude, 0.0001);
        Assert.AreEqual(-118.2437, result.Longitude, 0.0001);
        Assert.AreEqual("America/Los_Angeles", result.TimeZoneId);
        StringAssert.Contains(handler.LastUri!.Query, "Los%20Angeles");
    }

    [TestMethod]
    public async Task GetForecastAsync_UsesCityOffsetForOpenMeteoLocalWallClockTimes()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(
            """
            {
              "utc_offset_seconds": -28800,
              "timezone": "America/Los_Angeles",
              "current": {
                "temperature_2m": 12.0,
                "apparent_temperature": 11.0,
                "weather_code": 3
              },
              "hourly": {
                "time": ["2026-11-27T08:00", "2026-11-27T09:00"],
                "temperature_2m": [12.0, 10.0],
                "apparent_temperature": [11.0, 9.0],
                "precipitation_probability": [0, 80],
                "precipitation": [0.0, 0.2]
              }
            }
            """));
        using var client = new HttpClient(handler);
        var location = new CareLocation(
            "Los Angeles, California, United States",
            "Los Angeles",
            "United States",
            34.0522,
            -118.2437,
            "America/Los_Angeles",
            "manual",
            DateTimeOffset.UtcNow);

        var result = await new OpenMeteoWeatherService(client).GetForecastAsync(location);

        Assert.AreEqual(12.0, result.CurrentTemperatureC.GetValueOrDefault());
        Assert.AreEqual(11.0, result.CurrentApparentTemperatureC.GetValueOrDefault());
        Assert.AreEqual(2, result.UpcomingHours.Count);
        Assert.AreEqual(TimeSpan.FromHours(-8), result.UpcomingHours[0].Time.Offset);
        Assert.AreEqual(8, result.UpcomingHours[0].Time.Hour);
        Assert.AreEqual(80.0, result.UpcomingHours[1].PrecipitationProbability.GetValueOrDefault());
    }

    [TestMethod]
    public async Task GetForecastAsync_ParsesExplicitIso8601OffsetsWithoutConvertingToMachineTimezone()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(
            """
            {
              "utc_offset_seconds": 0,
              "current": { "temperature_2m": 10.0 },
              "hourly": {
                "time": ["2026-11-27T08:00:00-08:00"],
                "temperature_2m": [10.0]
              }
            }
            """));
        using var client = new HttpClient(handler);
        var location = new CareLocation(
            "Los Angeles, California, United States",
            "Los Angeles",
            "United States",
            34.0522,
            -118.2437,
            "America/Los_Angeles",
            "manual",
            DateTimeOffset.UtcNow);

        var result = await new OpenMeteoWeatherService(client).GetForecastAsync(location);

        Assert.AreEqual(TimeSpan.FromHours(-8), result.UpcomingHours[0].Time.Offset);
        Assert.AreEqual(8, result.UpcomingHours[0].Time.Hour);
    }

    [TestMethod]
    public async Task ResolveCityAsync_ReturnsNullWhenNoResultsExist()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json("""{"results":[]}"""));
        using var client = new HttpClient(handler);

        var result = await new OpenMeteoWeatherService(client).ResolveCityAsync("No Such City");

        Assert.IsNull(result);
    }
}
