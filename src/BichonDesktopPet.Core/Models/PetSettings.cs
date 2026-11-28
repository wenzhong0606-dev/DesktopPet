using System;

namespace BichonDesktopPet.Core.Models;

/// <summary>
/// User preferences persisted on the local machine.
/// Window coordinates are nullable so the application can choose a sensible first-run position.
/// </summary>
public sealed class PetSettings
{
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }

    /// <summary>The stable ID of the selected local avatar.</summary>
    public string ActiveAvatarId { get; set; } = "bichon";
    public bool AlwaysOnTop { get; set; } = true;
    public bool SnowEnabled { get; set; } = true;
    public bool DecorativeLightsEnabled { get; set; } = true;

    /// <summary>Music is deliberately disabled by default.</summary>
    public bool MusicEnabled { get; set; }

    /// <summary>Observe local input-idle duration and offer break reminders; no input content is collected.</summary>
    public bool ActivityAwarenessEnabled { get; set; } = true;

    /// <summary>Explicit opt-in to city-level IP geolocation with VPN/proxy assessment.</summary>
    public bool AutomaticIpLocationEnabled { get; set; }

    /// <summary>Explicit opt-in to weather-based care reminders.</summary>
    public bool WeatherRemindersEnabled { get; set; }

    /// <summary>Optional city-level override; takes precedence over automatic IP location.</summary>
    public string PreferredLocation { get; set; } = string.Empty;

    /// <summary>Last trusted coarse location cache. Raw IP addresses are never stored.</summary>
    public string? CachedLocationName { get; set; }
    public string? CachedLocationCity { get; set; }
    public string? CachedLocationCountry { get; set; }
    public double? CachedLocationLatitude { get; set; }
    public double? CachedLocationLongitude { get; set; }
    public string? CachedLocationTimeZoneId { get; set; }
    public string? CachedLocationSource { get; set; }
    public string? CachedLocationQuery { get; set; }
    public DateTimeOffset? CachedLocationUpdatedAtUtc { get; set; }

    /// <summary>Persisted per-type weather reminder cooldowns.</summary>
    public DateTimeOffset? LastColdWeatherReminderAtUtc { get; set; }
    public DateTimeOffset? LastRainReminderAtUtc { get; set; }
    public DateTimeOffset? LastHeatReminderAtUtc { get; set; }

    public DateTimeOffset[] CareDeliveryHistoryUtc { get; set; } = [];

    public double WindowWidth { get; set; } = 280;
    public double WindowHeight { get; set; } = 360;
}
