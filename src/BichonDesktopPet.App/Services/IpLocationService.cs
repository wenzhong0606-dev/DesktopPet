using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;

namespace BichonDesktopPet.App.Services;

public sealed record IpLocationResult(CareLocation? Location, string? RejectionReason)
{
    public bool IsTrusted => Location is not null;
}

/// <summary>
/// Uses IPLogs' caller-check endpoint. The user's public IP is visible to that provider,
/// but is never copied into local settings, logs or chat history.
/// </summary>
public sealed class IpLocationService
{
    private static readonly Uri Endpoint = new("https://iplogs.com/v1/check");
    private readonly HttpClient _httpClient;

    public IpLocationService(HttpClient httpClient) =>
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    public async Task<IpLocationResult> ResolveCurrentAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        var score = ReadDouble(root, "score");
        var verdict = ReadString(root, "verdict");
        var isVpn = ReadBoolean(root, "is_vpn");
        var isProxy = ReadBoolean(root, "is_proxy");
        var isTor = ReadBoolean(root, "is_tor");
        var isRelay = ReadBoolean(root, "is_relay");
        var isHosting = ReadBoolean(root, "is_hosting");
        var isSatellite = ReadBoolean(root, "is_satellite");

        if (!LocationTrustPolicy.IsTrusted(score, verdict, isVpn, isProxy, isTor, isRelay, isHosting, isSatellite))
        {
            return new IpLocationResult(null, "当前网络出口被识别为 VPN、代理、中继、数据中心或可信度不足；未采用出口位置。");
        }

        if (!root.TryGetProperty("ip_info", out var ipInfo) || ipInfo.ValueKind != JsonValueKind.Object)
        {
            return new IpLocationResult(null, "位置服务没有返回可用的城市信息。");
        }

        var city = ReadString(ipInfo, "city");
        var country = ReadString(ipInfo, "country");
        var latitude = ReadDouble(ipInfo, "lat");
        var longitude = ReadDouble(ipInfo, "lon");
        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(country) ||
            latitude is null || longitude is null ||
            latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            return new IpLocationResult(null, "IP 位置结果缺少有效城市坐标。");
        }

        var location = new CareLocation(
            $"{city}, {country}",
            city,
            country,
            latitude.Value,
            longitude.Value,
            "auto",
            "ip",
            DateTimeOffset.UtcNow);

        return new IpLocationResult(location, null);
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

    private static bool? ReadBoolean(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) &&
        (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False)
            ? value.GetBoolean()
            : null;
}
