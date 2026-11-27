using BichonDesktopPet.Core.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class PetSettingsTests
{
    [TestMethod]
    public void NewSettings_UseTheApprovedWindowSize()
    {
        var settings = new PetSettings();

        Assert.AreEqual(280d, settings.WindowWidth);
        Assert.AreEqual(360d, settings.WindowHeight);
    }

    [TestMethod]
    public void NewSettings_SelectTheBuiltInBichonByDefault()
    {
        var settings = new PetSettings();

        Assert.AreEqual("bichon", settings.ActiveAvatarId);
    }

    [TestMethod]
    public void Music_IsDisabledByDefault()
    {
        var settings = new PetSettings();

        Assert.IsFalse(settings.MusicEnabled);
    }

    [TestMethod]
    public void DesktopPet_IsAlwaysOnTopByDefault()
    {
        var settings = new PetSettings();

        Assert.IsTrue(settings.AlwaysOnTop);
    }
}
