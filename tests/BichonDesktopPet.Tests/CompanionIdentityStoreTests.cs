using System;
using System.IO;
using System.Text.Json;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class CompanionIdentityStoreTests
{
    private string _testDirectory = null!;
    private CompanionIdentityStore _store = null!;

    [TestInitialize]
    public void SetUp()
    {
        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            "BichonDesktopPet.CompanionTests",
            Guid.NewGuid().ToString("N"));
        _store = new CompanionIdentityStore(Path.Combine(_testDirectory, "companions.db"));
        _store.Initialize();
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
    public void EnsureCompanion_CreatesStableIdentityForAvatar()
    {
        var avatar = CreateAvatar("avatar-1", "豆包");
        var first = _store.EnsureCompanion(
            avatar,
            new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero));
        var second = _store.EnsureCompanion(
            avatar,
            new DateTimeOffset(2026, 11, 2, 8, 0, 0, TimeSpan.Zero));

        Assert.AreEqual(first.Id, second.Id);
        Assert.AreEqual("avatar-1", second.AvatarId);
        Assert.AreEqual("豆包", second.DisplayName);
        Assert.AreEqual(first.CreatedAtUtc, second.CreatedAtUtc);
        Assert.IsTrue(second.UpdatedAtUtc > first.UpdatedAtUtc);
        Assert.IsTrue(JsonDocument.Parse(second.CorePersonalityJson).RootElement
            .GetProperty("selfReference").GetString() == "豆包");
        Assert.AreEqual("{}", second.EvolvingPersonalityJson);
    }

    [TestMethod]
    public void EnsureCompanion_RefreshesCoreProfileWithoutChangingIdentity()
    {
        var first = _store.EnsureCompanion(CreateAvatar("avatar-1", "豆包"));
        var renamed = _store.EnsureCompanion(CreateAvatar("avatar-1", "豆包同学"));

        Assert.AreEqual(first.Id, renamed.Id);
        Assert.AreEqual("豆包同学", renamed.DisplayName);
        Assert.AreEqual(first.CreatedAtUtc, renamed.CreatedAtUtc);
        Assert.IsTrue(JsonDocument.Parse(renamed.CorePersonalityJson).RootElement
            .GetProperty("selfReference").GetString() == "豆包同学");
    }

    [TestMethod]
    public void RecordInteraction_PersistsEventsForTheCorrectCompanion()
    {
        var companion = _store.EnsureCompanion(CreateAvatar("avatar-1", "豆包"));
        var occurredAt = new DateTimeOffset(2026, 11, 27, 9, 30, 0, TimeSpan.Zero);

        _store.RecordInteraction(
            companion.Id,
            "pet_clicked",
            "{\"source\":\"desktop\"}",
            occurredAt);

        var events = _store.GetRecentInteractions(companion.Id);

        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(companion.Id, events[0].CompanionId);
        Assert.AreEqual("pet_clicked", events[0].EventType);
        Assert.AreEqual("{\"source\":\"desktop\"}", events[0].PayloadJson);
        Assert.AreEqual(occurredAt, events[0].OccurredAtUtc);
    }

    [TestMethod]
    public void ArchiveByAvatarId_PreservesIdentityAndHistoryButHidesActiveMapping()
    {
        var companion = _store.EnsureCompanion(CreateAvatar("avatar-1", "豆包"));
        _store.RecordInteraction(companion.Id, "pet_clicked");

        _store.ArchiveByAvatarId("avatar-1");

        Assert.IsNull(_store.GetByAvatarId("avatar-1"));
        Assert.AreEqual(1, _store.GetRecentInteractions(companion.Id).Count);
    }

    [TestMethod]
    public void RecordInteraction_RejectsInvalidJsonPayload()
    {
        var companion = _store.EnsureCompanion(CreateAvatar("avatar-1", "豆包"));

        Assert.ThrowsException<JsonReaderException>(() =>
            _store.RecordInteraction(companion.Id, "pet_clicked", "{not-json"));
    }

    [TestMethod]
    public void GetRecentInteractions_EnforcesBoundedLimit()
    {
        var companion = _store.EnsureCompanion(CreateAvatar("avatar-1", "豆包"));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            _store.GetRecentInteractions(companion.Id, 101));
    }

    private static AvatarProfile CreateAvatar(string id, string name) => new()
    {
        Id = id,
        Name = name,
        Species = "dog",
        Persona = new PersonaProfile
        {
            SelfReference = name,
            UserAddress = "主人",
            PersonalityTraits = ["活泼"]
        }
    };
}
