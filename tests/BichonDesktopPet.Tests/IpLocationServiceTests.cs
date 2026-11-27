using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BichonDesktopPet.App.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class IpLocationServiceTests
{
    [TestMethod]
    public async Task ResolveCurrentAsync_PostsWithoutIpAndReadsTrustedCity()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(
            """
            {
              "score": 0.05,
              "verdict": "clean",
              "is_vpn": false,
              "is_proxy": false,
              "is_tor": false,
              "is_relay": false,
              "is_hosting": false,
              "is_satellite": false,
              "city": "Los Angeles",
              "country": "United States"
            }
            """));
        using var client = new HttpClient(handler);

        var result = await new IpLocationService(client).ResolveCurrentAsync();

        Assert.IsTrue(result.IsTrusted);
        Assert.AreEqual("Los Angeles", result.City);
        Assert.AreEqual("United States", result.Country);
        Assert.AreEqual(HttpMethod.Post, handler.LastMethod);
        Assert.AreEqual("https://iplogs.com/v1/check", handler.LastUri!.AbsoluteUri);
        Assert.AreEqual("{}", handler.LastRequestBody);
    }

    [TestMethod]
    public async Task ResolveCurrentAsync_ReadsNestedIpInfoAndRejectsVpnExit()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(
            """
            {
              "score": 1.0,
              "verdict": "vpn_detected",
              "is_vpn": true,
              "is_proxy": true,
              "ip_info": {
                "city": "Frankfurt",
                "country": "Germany"
              }
            }
            """));
        using var client = new HttpClient(handler);

        var result = await new IpLocationService(client).ResolveCurrentAsync();

        Assert.IsFalse(result.IsTrusted);
        Assert.IsNull(result.City);
        StringAssert.Contains(result.RejectionReason!, "未采用出口位置");
    }

    [TestMethod]
    public async Task ResolveCurrentAsync_ReadsCityAndCountryFromIpInfo()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(
            """
            {
              "score": 0.02,
              "verdict": "clean",
              "is_vpn": false,
              "ip_info": {
                "city": "Los Angeles",
                "country": "United States"
              }
            }
            """));
        using var client = new HttpClient(handler);

        var result = await new IpLocationService(client).ResolveCurrentAsync();

        Assert.IsTrue(result.IsTrusted);
        Assert.AreEqual("Los Angeles", result.City);
        Assert.AreEqual("United States", result.Country);
    }

    [TestMethod]
    public async Task ResolveCurrentAsync_RejectsIncompleteOrSuspiciousResponse()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(
            """
            {
              "score": 0.05,
              "verdict": "clean",
              "city": "Los Angeles",
              "country": "United States"
            }
            """));
        using var client = new HttpClient(handler);

        var result = await new IpLocationService(client).ResolveCurrentAsync();

        Assert.IsFalse(result.IsTrusted);
        Assert.IsNotNull(result.RejectionReason);
    }

    [TestMethod]
    public async Task ResolveCurrentAsync_PropagatesHttpErrorsForCallerToHandle()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(
            """{"error":"rate limited"}""",
            HttpStatusCode.TooManyRequests));
        using var client = new HttpClient(handler);

        await Assert.ThrowsExceptionAsync<HttpRequestException>(
            () => new IpLocationService(client).ResolveCurrentAsync());
    }
}
