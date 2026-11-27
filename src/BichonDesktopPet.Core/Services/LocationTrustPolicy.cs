namespace BichonDesktopPet.Core.Services;

/// <summary>
/// Fail-closed decision for whether an IP-derived city is suitable for personal weather hints.
/// </summary>
public static class LocationTrustPolicy
{
    public static bool IsTrusted(
        double? score,
        string? verdict,
        bool? isVpn,
        bool? isProxy,
        bool? isTor,
        bool? isRelay,
        bool? isHosting,
        bool? isSatellite)
    {
        return score is >= 0 and < 0.30 &&
            string.Equals(verdict, "clean", System.StringComparison.OrdinalIgnoreCase) &&
            isVpn == false &&
            isProxy != true &&
            isTor != true &&
            isRelay != true &&
            isHosting != true &&
            isSatellite != true;
    }

    /// <summary>
    /// A large offset mismatch between Windows and an IP-derived city is a soft signal
    /// that the network exit may not represent the user's actual location.
    /// </summary>
    public static bool IsTimeZoneConsistent(TimeSpan systemOffset, TimeSpan cityOffset) =>
        System.Math.Abs((systemOffset - cityOffset).TotalMinutes) < 90;
}
