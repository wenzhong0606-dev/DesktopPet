using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class LocationTrustPolicyTests
{
    [TestMethod]
    public void IsTrusted_AcceptsOnlyCleanNonAnonymizedNetwork()
    {
        Assert.IsTrue(LocationTrustPolicy.IsTrusted(0.05, "clean", false, false, false, false, false, false));
    }

    [DataTestMethod]
    [DataRow(0.60, "vpn_detected", true, false, false, false, false, false)]
    [DataRow(0.35, "suspicious", false, false, false, false, false, false)]
    [DataRow(0.05, "clean", false, true, false, false, false, false)]
    [DataRow(0.05, "clean", false, false, true, false, false, false)]
    [DataRow(0.05, "clean", false, false, false, true, false, false)]
    [DataRow(0.05, "clean", false, false, false, false, true, false)]
    [DataRow(0.05, "clean", false, false, false, false, false, true)]
    public void IsTrusted_RejectsAnonymizedOrUncertainNetwork(
        double score, string verdict, bool isVpn, bool isProxy, bool isTor,
        bool isRelay, bool isHosting, bool isSatellite)
    {
        Assert.IsFalse(LocationTrustPolicy.IsTrusted(
            score, verdict, isVpn, isProxy, isTor, isRelay, isHosting, isSatellite));
    }

    [TestMethod]
    public void IsTrusted_RequiresCoreFieldsAndAllowsOmittedOptionalFlags()
    {
        Assert.IsFalse(LocationTrustPolicy.IsTrusted(null, "clean", false, false, false, false, false, false));
        Assert.IsFalse(LocationTrustPolicy.IsTrusted(0.05, null, false, false, false, false, false, false));
        Assert.IsFalse(LocationTrustPolicy.IsTrusted(0.05, "clean", null, false, false, false, false, false));
        Assert.IsTrue(LocationTrustPolicy.IsTrusted(0.05, "clean", false, null, null, null, null, null));
    }
}
