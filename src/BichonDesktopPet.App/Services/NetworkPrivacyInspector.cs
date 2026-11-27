using System;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;

namespace BichonDesktopPet.App.Services;

/// <summary>
/// Conservative local checks for configured system proxies and active VPN/tunnel adapters.
/// This is a safety gate, not a claim that every VPN can be detected locally.
/// </summary>
public sealed class NetworkPrivacyInspector
{
    private static readonly string[] AdapterMarkers =
    [
        "wireguard", "wintun", "openvpn", "tap-windows", "nordlynx", "nordvpn",
        "expressvpn", "surfshark", "protonvpn", "mullvad", "tailscale", "zerotier",
        "cloudflare warp", "warp", "globalprotect", "anyconnect", "forticlient", "ikev2", "sstp", "l2tp", "pptp",
        "pulse secure", "ivanti", "zscaler", "netskope", "private internet access"
    ];

    public string? GetPotentialAnonymizationReason()
    {
        try
        {
            var target = new Uri("https://iplogs.com");
            if (!HttpClient.DefaultProxy.IsBypassed(target))
            {
                return "检测到 Windows 系统代理配置";
            }
        }
        catch
        {
            // Continue with local adapter checks; a proxy inspection failure is not proof of a direct route.
        }

        try
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up ||
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                var description = $"{adapter.Name} {adapter.Description}";
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                    AdapterMarkers.Any(marker => description.Contains(marker, StringComparison.OrdinalIgnoreCase)))
                {
                    return "检测到可能的 VPN 或隧道网络接口";
                }
            }
        }
        catch
        {
            return "无法确认当前网络路由是否经过代理或 VPN";
        }

        return null;
    }
}
