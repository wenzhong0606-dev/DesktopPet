using System;
using System.IO;
using System.Text.Json;
using BichonDesktopPet.Core.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class QwenOptionsLoaderTests
{
    private string _testDirectory = null!;

    [TestInitialize]
    public void SetUp()
    {
        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            "BichonDesktopPet.QwenOptionsTests",
            Guid.NewGuid().ToString("N"));
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
    public void Load_WhenNoConfigurationExists_UsesQwenPlusDefaultsAndRemainsDisabled()
    {
        var options = QwenOptionsLoader.Load(_testDirectory, _ => null);

        Assert.AreEqual(QwenOptions.DefaultEndpoint, options.Endpoint);
        Assert.AreEqual("qwen-plus", options.Model);
        Assert.IsFalse(options.IsConfigured);
        Assert.AreEqual(string.Empty, options.ApiKey);
    }

    [TestMethod]
    public void Load_ReadsLocalSettingsWithoutRequiringEnvironmentVariables()
    {
        File.WriteAllText(
            Path.Combine(_testDirectory, QwenOptionsLoader.LocalFileName),
            """
            {
              "Qwen": {
                "ApiKey": "local-secret",
                "Endpoint": "https://example.invalid/v1/chat/completions",
                "Model": "qwen-plus"
              }
            }
            """);

        var options = QwenOptionsLoader.Load(_testDirectory, _ => null);

        Assert.AreEqual("local-secret", options.ApiKey);
        Assert.AreEqual("https://example.invalid/v1/chat/completions", options.Endpoint);
        Assert.AreEqual("qwen-plus", options.Model);
        Assert.IsTrue(options.IsConfigured);
    }

    [TestMethod]
    public void Load_EnvironmentVariablesOverrideLocalSettings()
    {
        File.WriteAllText(
            Path.Combine(_testDirectory, QwenOptionsLoader.LocalFileName),
            """
            {
              "Qwen": {
                "ApiKey": "file-secret",
                "Endpoint": "https://file.example/v1/chat/completions",
                "Model": "qwen-plus"
              }
            }
            """);

        var options = QwenOptionsLoader.Load(_testDirectory, name => name switch
        {
            "Qwen__ApiKey" => "environment-secret",
            "Qwen__Endpoint" => "https://environment.example/v1/chat/completions",
            "Qwen__Model" => "qwen-plus",
            _ => null
        });

        Assert.AreEqual("environment-secret", options.ApiKey);
        Assert.AreEqual("https://environment.example/v1/chat/completions", options.Endpoint);
    }

    [TestMethod]
    public void Load_WhenLocalFileIsMalformed_ReportsJsonError()
    {
        File.WriteAllText(
            Path.Combine(_testDirectory, QwenOptionsLoader.LocalFileName),
            "{ invalid");

        try
        {
            QwenOptionsLoader.Load(_testDirectory, _ => null);
            Assert.Fail("Malformed local settings should be rejected.");
        }
        catch (JsonException)
        {
            // JsonReaderException derives from JsonException.
        }
    }
}
