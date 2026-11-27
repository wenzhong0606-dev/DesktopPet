using System;

namespace BichonDesktopPet.Core.Models;

/// <summary>
/// Coarse city-level location used for weather only. Never contains a street address or raw IP.
/// </summary>
public sealed record CareLocation(
    string DisplayName,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    string TimeZoneId,
    string Source,
    DateTimeOffset UpdatedAtUtc);
