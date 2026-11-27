using System;
using System.Collections.Generic;
using System.IO;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class SettingsServiceTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "BichonDesktopPet.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDirectory);
    }

    [TestCleanup]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void SaveAndLoad_RoundTripsUserPreferencesAndCreatesDirectory()
    {
        var settingsPath = Path.Combine(_testDirectory, "nested", "settings.json");
        var service = new SettingsService(settingsPath);
        var expected = new PetSettings
        {
            WindowLeft = 123.5,
            WindowTop = 456.5,
            AlwaysOnTop = false,
            SnowEnabled = false,
            DecorativeLightsEnabled = true,
            MusicEnabled = false,
            WindowWidth = 280,
            WindowHeight = 360
        };

        Assert.IsTrue(service.Save(expected));
        var actual = service.Load();

        Assert.IsTrue(File.Exists(settingsPath));
        Assert.AreEqual(expected.WindowLeft, actual.WindowLeft);
        Assert.AreEqual(expected.WindowTop, actual.WindowTop);
        Assert.AreEqual(expected.AlwaysOnTop, actual.AlwaysOnTop);
        Assert.AreEqual(expected.SnowEnabled, actual.SnowEnabled);
        Assert.AreEqual(expected.DecorativeLightsEnabled, actual.DecorativeLightsEnabled);
        Assert.AreEqual(expected.MusicEnabled, actual.MusicEnabled);
    }

    [TestMethod]
    public void Load_WhenFileDoesNotExist_ReturnsDefaults()
    {
        var settingsPath = Path.Combine(_testDirectory, "not-created.json");
        var service = new SettingsService(settingsPath);

        var settings = service.Load();

        Assert.IsTrue(settings.AlwaysOnTop);
        Assert.IsTrue(settings.SnowEnabled);
        Assert.IsTrue(settings.DecorativeLightsEnabled);
        Assert.IsFalse(settings.MusicEnabled);
    }

    [TestMethod]
    public void Load_WhenJsonIsCorrupt_ReturnsDefaultsAndReportsFailure()
    {
        var settingsPath = Path.Combine(_testDirectory, "settings.json");
        File.WriteAllText(settingsPath, "{ this is not valid JSON");
        var loggedMessages = new List<string>();
        var service = new SettingsService(settingsPath, (message, _) => loggedMessages.Add(message));

        var settings = service.Load();

        Assert.IsTrue(settings.AlwaysOnTop);
        Assert.IsTrue(settings.SnowEnabled);
        Assert.IsTrue(settings.DecorativeLightsEnabled);
        Assert.IsFalse(settings.MusicEnabled);
        Assert.AreEqual(1, loggedMessages.Count);
        StringAssert.Contains(loggedMessages[0], "Unable to read");
    }
    [TestMethod]
    public void Save_WhenTargetIsDirectory_ReportsFailureWithoutThrowing()
    {
        var loggedMessages = new List<string>();
        var service = new SettingsService(_testDirectory, (message, _) => loggedMessages.Add(message));

        Assert.IsFalse(service.Save(new PetSettings()));

        Assert.AreEqual(1, loggedMessages.Count);
        StringAssert.Contains(loggedMessages[0], "Unable to save");
    }

}
