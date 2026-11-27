using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class AvatarCatalogServiceTests
{
    private const string OnePixelPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j5ZkAAAAASUVORK5CYII=";

    private string _testDirectory = null!;
    private string _assetsDirectory = null!;
    private string _userDataDirectory = null!;

    [TestInitialize]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "BichonDesktopPet.AvatarTests", Guid.NewGuid().ToString("N"));
        _assetsDirectory = Path.Combine(_testDirectory, "Assets");
        _userDataDirectory = Path.Combine(_testDirectory, "UserData");

        var bichonDirectory = Path.Combine(_assetsDirectory, "characters", "bichon", "base");
        Directory.CreateDirectory(bichonDirectory);
        File.WriteAllBytes(Path.Combine(bichonDirectory, "bichon-idle.png"), Convert.FromBase64String(OnePixelPngBase64));
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
    public void Load_WhenCatalogDoesNotExist_SeedsBuiltInBichon()
    {
        var service = CreateService();

        var avatars = service.Load();

        Assert.AreEqual(1, avatars.Count);
        Assert.AreEqual(AvatarCatalogService.DefaultAvatarId, avatars[0].Id);
        Assert.AreEqual("比熊", avatars[0].Name);
        Assert.IsTrue(avatars[0].IsBuiltIn);
        Assert.IsTrue(File.Exists(service.CatalogPath));
        Assert.IsNotNull(service.ResolveImagePath(AvatarCatalogService.DefaultAvatarId));
    }

    [TestMethod]
    public void ImportImage_CopiesImageAndPersistsProfileAcrossReload()
    {
        var sourcePath = Path.Combine(_testDirectory, "border-collie.png");
        File.WriteAllBytes(sourcePath, Convert.FromBase64String(OnePixelPngBase64));

        var service = CreateService();
        service.Load();

        var imported = service.ImportImage(sourcePath, "边牧", "dog");
        var storedImagePath = service.ResolveImagePath(imported.Id);

        Assert.IsFalse(imported.IsBuiltIn);
        Assert.AreEqual("边牧", imported.Name);
        Assert.AreEqual("dog", imported.Species);
        Assert.IsNotNull(storedImagePath);
        Assert.IsTrue(File.Exists(storedImagePath));

        var reloadedService = CreateService();
        var avatars = reloadedService.Load();

        Assert.AreEqual(2, avatars.Count);
        Assert.IsNotNull(reloadedService.GetAvatar(imported.Id));
        Assert.IsNotNull(reloadedService.ResolveImagePath(imported.Id));
    }

    [TestMethod]
    public void ImportImage_RejectsFileWithMismatchedImageSignature()
    {
        var sourcePath = Path.Combine(_testDirectory, "not-an-image.png");
        File.WriteAllText(sourcePath, "this is not a PNG image");
        var service = CreateService();
        service.Load();

        Assert.ThrowsException<InvalidDataException>(
            () => service.ImportImage(sourcePath, "伪图片", "custom"));
    }

    [TestMethod]
    public void ImportImage_RejectsUnsupportedExtension()
    {
        var sourcePath = Path.Combine(_testDirectory, "image.webp");
        File.WriteAllBytes(sourcePath, Convert.FromBase64String(OnePixelPngBase64));
        var service = CreateService();
        service.Load();

        Assert.ThrowsException<InvalidDataException>(
            () => service.ImportImage(sourcePath, "图片", "custom"));
    }

    [TestMethod]
    public void ImportImage_RejectsFilesLargerThanTwentyMegabytes()
    {
        var sourcePath = Path.Combine(_testDirectory, "oversized.png");
        using (var stream = File.Create(sourcePath))
        {
            stream.SetLength(AvatarCatalogService.MaximumImageBytes + 1);
        }

        var service = CreateService();
        service.Load();

        Assert.ThrowsException<InvalidDataException>(
            () => service.ImportImage(sourcePath, "超大图片", "custom"));
    }

    [TestMethod]
    public void Delete_CannotDeleteBuiltInBichon()
    {
        var service = CreateService();
        service.Load();

        Assert.IsFalse(service.Delete(AvatarCatalogService.DefaultAvatarId));
        Assert.AreEqual(1, service.GetAvatars().Count);
    }

    [TestMethod]
    public void Delete_RemovesImportedAvatarAndKeepsBuiltInDefault()
    {
        var sourcePath = Path.Combine(_testDirectory, "cat.png");
        File.WriteAllBytes(sourcePath, Convert.FromBase64String(OnePixelPngBase64));
        var service = CreateService();
        service.Load();

        var imported = service.ImportImage(sourcePath, "猫咪", "cat");
        var storedImagePath = service.ResolveImagePath(imported.Id);

        Assert.IsTrue(service.Delete(imported.Id));
        Assert.AreEqual(1, service.GetAvatars().Count);
        Assert.AreEqual(AvatarCatalogService.DefaultAvatarId, service.GetAvatars()[0].Id);
        Assert.IsFalse(File.Exists(storedImagePath));
    }

    [TestMethod]
    public void Load_WhenCatalogIsCorrupt_RestoresDefaultAndReportsFailure()
    {
        var avatarDirectory = Path.Combine(_userDataDirectory, "avatars");
        Directory.CreateDirectory(avatarDirectory);
        File.WriteAllText(Path.Combine(avatarDirectory, "catalog.json"), "{ invalid json");

        var messages = new List<string>();
        var service = new AvatarCatalogService(_assetsDirectory, _userDataDirectory, (message, _) => messages.Add(message));

        var avatars = service.Load();

        Assert.AreEqual(1, avatars.Count);
        Assert.AreEqual(AvatarCatalogService.DefaultAvatarId, avatars[0].Id);
        Assert.AreEqual(1, messages.Count);
        StringAssert.Contains(messages[0], "avatar catalog");
    }

    [TestMethod]
    public void Load_WhenSchemaVersionOne_MigratesWithoutChangingAvatarIdsImagesOrSettings()
    {
        var avatarId = Guid.NewGuid().ToString("N");
        var avatarFolder = Path.Combine(_userDataDirectory, "avatars", avatarId);
        Directory.CreateDirectory(avatarFolder);
        var imagePath = Path.Combine(avatarFolder, "avatar.png");
        File.WriteAllBytes(imagePath, Convert.FromBase64String(OnePixelPngBase64));

        var catalogDirectory = Path.Combine(_userDataDirectory, "avatars");
        Directory.CreateDirectory(catalogDirectory);
        var catalogPath = Path.Combine(catalogDirectory, "catalog.json");
        var legacyCatalog = new
        {
            SchemaVersion = 1,
            Avatars = new[]
            {
                new
                {
                    Id = "bichon",
                    Name = "比熊",
                    Species = "dog",
                    ImagePath = "characters/bichon/base/bichon-idle.png",
                    IsBuiltIn = true,
                    CreatedAtUtc = DateTime.UnixEpoch
                },
                new
                {
                    Id = avatarId,
                    Name = "边牧",
                    Species = "dog",
                    ImagePath = avatarId + "/avatar.png",
                    IsBuiltIn = false,
                    CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            }
        };
        File.WriteAllText(catalogPath, JsonSerializer.Serialize(legacyCatalog));

        var settingsPath = Path.Combine(_userDataDirectory, "settings.json");
        const string settingsJson = "{\"ActiveAvatarId\":\"preserve-this-selection\"}";
        Directory.CreateDirectory(_userDataDirectory);
        File.WriteAllText(settingsPath, settingsJson);

        var service = CreateService();
        var avatars = service.Load();
        var migrated = service.GetAvatar(avatarId);

        Assert.AreEqual(2, avatars.Count);
        Assert.IsNotNull(migrated);
        Assert.AreEqual("边牧", migrated.Name);
        Assert.AreEqual("dog", migrated.Species);
        Assert.AreEqual(avatarId + "/avatar.png", migrated.ImagePath);
        Assert.IsNotNull(migrated.Persona);
        Assert.AreEqual("我", migrated.Persona.SelfReference);
        Assert.IsTrue(File.Exists(imagePath));
        Assert.AreEqual(settingsJson, File.ReadAllText(settingsPath));

        using var document = JsonDocument.Parse(File.ReadAllText(catalogPath));
        Assert.AreEqual(
            AvatarCatalogService.CurrentSchemaVersion,
            document.RootElement.GetProperty("SchemaVersion").GetInt32());
    }

    [TestMethod]
    public void UpdateProfile_PersistsIdentityAndPersonaAcrossReload()
    {
        var service = CreateService();
        service.Load();

        var persona = new PersonaProfile
        {
            Nicknames = ["小豆", "豆豆"],
            SelfReference = "豆包",
            UserAddress = "主人",
            PersonalityTraits = ["活泼", "黏人", "好奇"],
            SpeechStyle = "playful",
            ResponseLength = "medium",
            EmojiLevel = "high",
            Catchphrases = ["汪！豆包来啦！"]
        };

        var updated = service.UpdateProfile(
            AvatarCatalogService.DefaultAvatarId,
            "豆包",
            "dog",
            "比熊",
            persona);

        var reloaded = CreateService();
        reloaded.Load();
        var restored = reloaded.GetAvatar(AvatarCatalogService.DefaultAvatarId);

        Assert.IsNotNull(restored);
        Assert.AreEqual("豆包", restored.Name);
        Assert.AreEqual("比熊", restored.Breed);
        Assert.AreEqual("豆包", restored.Persona.SelfReference);
        CollectionAssert.AreEqual(new[] { "小豆", "豆豆" }, restored.Persona.Nicknames);
        CollectionAssert.AreEqual(new[] { "活泼", "黏人", "好奇" }, restored.Persona.PersonalityTraits);
        Assert.AreEqual("playful", restored.Persona.SpeechStyle);
        Assert.AreEqual("medium", restored.Persona.ResponseLength);
        Assert.AreEqual("high", restored.Persona.EmojiLevel);
        CollectionAssert.AreEqual(new[] { "汪！豆包来啦！" }, restored.Persona.Catchphrases);
        Assert.AreEqual(updated.ImagePath, restored.ImagePath);
        Assert.IsTrue(restored.IsBuiltIn);
    }

    [TestMethod]
    public void UpdateProfile_RejectsMoreThanThreeTraitsAndKeepsPreviousProfile()
    {
        var service = CreateService();
        service.Load();

        var invalidPersona = new PersonaProfile
        {
            PersonalityTraits = ["活泼", "黏人", "好奇", "调皮"]
        };

        Assert.ThrowsException<ArgumentException>(() => service.UpdateProfile(
            AvatarCatalogService.DefaultAvatarId,
            "不应保存",
            "dog",
            "比熊",
            invalidPersona));

        var unchanged = service.GetAvatar(AvatarCatalogService.DefaultAvatarId);
        Assert.IsNotNull(unchanged);
        Assert.AreEqual("比熊", unchanged.Name);
    }

    private AvatarCatalogService CreateService() =>
        new(_assetsDirectory, _userDataDirectory);
}
