using System;
using System.IO;
using System.Linq;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class ChatHistoryStoreTests
{
    private string _testDirectory = null!;
    private string _databasePath = null!;
    private CompanionIdentityStore _identityStore = null!;
    private ChatHistoryStore _historyStore = null!;
    private CompanionIdentity _companion = null!;

    [TestInitialize]
    public void SetUp()
    {
        _testDirectory = Path.Combine(
            Path.GetTempPath(),
            "BichonDesktopPet.ChatHistoryTests",
            Guid.NewGuid().ToString("N"));
        _databasePath = Path.Combine(_testDirectory, "companions.db");

        _identityStore = new CompanionIdentityStore(_databasePath);
        _identityStore.Initialize();
        _companion = _identityStore.EnsureCompanion(CreateAvatar("avatar-1", "豆包"));

        _historyStore = new ChatHistoryStore(_databasePath);
        _historyStore.Initialize();
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
    public void GetMessagePage_UsesBoundedKeysetPaginationInChronologicalOrder()
    {
        var session = _historyStore.CreateSession(_companion.Id);
        var start = new DateTimeOffset(2026, 11, 27, 8, 0, 0, TimeSpan.Zero);

        for (var index = 0; index < 65; index++)
        {
            _historyStore.SaveMessage(
                _companion.Id,
                session.Id,
                index % 2 == 0 ? "user" : "assistant",
                $"message-{index}",
                start.AddSeconds(index));
        }

        var newest = _historyStore.GetMessagePage(_companion.Id, session.Id, pageSize: 30);
        Assert.AreEqual(30, newest.Messages.Count);
        Assert.IsTrue(newest.HasMore);
        Assert.AreEqual("message-35", newest.Messages[0].Content);
        Assert.AreEqual("message-64", newest.Messages[29].Content);

        var middle = _historyStore.GetMessagePage(
            _companion.Id,
            session.Id,
            pageSize: 30,
            beforeMessageId: newest.NextBeforeMessageId);
        Assert.AreEqual(30, middle.Messages.Count);
        Assert.IsTrue(middle.HasMore);
        Assert.AreEqual("message-5", middle.Messages[0].Content);
        Assert.AreEqual("message-34", middle.Messages[29].Content);

        var oldest = _historyStore.GetMessagePage(
            _companion.Id,
            session.Id,
            pageSize: 30,
            beforeMessageId: middle.NextBeforeMessageId);
        Assert.AreEqual(5, oldest.Messages.Count);
        Assert.IsFalse(oldest.HasMore);
        Assert.AreEqual("message-0", oldest.Messages[0].Content);
        Assert.AreEqual("message-4", oldest.Messages[4].Content);
    }

    [TestMethod]
    public void SaveMessage_SetsTitleFromFirstUserMessageAndUpdatesSessionActivity()
    {
        var session = _historyStore.CreateSession(
            _companion.Id,
            new DateTimeOffset(2026, 11, 27, 8, 0, 0, TimeSpan.Zero));
        var firstMessageAt = new DateTimeOffset(2026, 11, 27, 8, 1, 0, TimeSpan.Zero);

        _historyStore.SaveMessage(
            _companion.Id,
            session.Id,
            "user",
            "你好\n我是豆包",
            firstMessageAt);
        _historyStore.SaveMessage(
            _companion.Id,
            session.Id,
            "assistant",
            "你好呀！",
            firstMessageAt.AddSeconds(1));
        _historyStore.SaveMessage(
            _companion.Id,
            session.Id,
            "user",
            "继续聊聊",
            firstMessageAt.AddSeconds(2));

        var stored = _historyStore.GetSessions(_companion.Id).Single();
        Assert.AreEqual("你好 我是豆包", stored.Title);
        Assert.AreEqual(3L, stored.MessageCount);
        Assert.AreEqual(firstMessageAt.AddSeconds(2), stored.UpdatedAtUtc);
    }

    [TestMethod]
    public void ChatHistory_IsIsolatedByCompanionAndClearOnlyDeletesSelectedCompanion()
    {
        var otherCompanion = _identityStore.EnsureCompanion(CreateAvatar("avatar-2", "小猫"));
        var firstSession = _historyStore.CreateSession(_companion.Id);
        var otherSession = _historyStore.CreateSession(otherCompanion.Id);

        _historyStore.SaveMessage(_companion.Id, firstSession.Id, "user", "豆包的消息");
        _historyStore.SaveMessage(otherCompanion.Id, otherSession.Id, "user", "小猫的消息");

        Assert.AreEqual(1, _historyStore.GetSessions(_companion.Id).Count);
        Assert.AreEqual(1, _historyStore.GetSessions(otherCompanion.Id).Count);

        Assert.AreEqual(1, _historyStore.DeleteAllSessions(_companion.Id));
        Assert.AreEqual(0, _historyStore.GetSessions(_companion.Id).Count);
        Assert.AreEqual(1, _historyStore.GetSessions(otherCompanion.Id).Count);

        var otherPage = _historyStore.GetMessagePage(otherCompanion.Id, otherSession.Id);
        Assert.AreEqual("小猫的消息", otherPage.Messages.Single().Content);
    }

    [TestMethod]
    public void DeleteSession_CascadesMessagesAndCannotDeleteAnotherCompanionsSession()
    {
        var otherCompanion = _identityStore.EnsureCompanion(CreateAvatar("avatar-2", "小猫"));
        var session = _historyStore.CreateSession(_companion.Id);
        _historyStore.SaveMessage(_companion.Id, session.Id, "user", "需要删除");

        Assert.IsFalse(_historyStore.DeleteSession(otherCompanion.Id, session.Id));
        Assert.AreEqual(1, _historyStore.GetMessagePage(_companion.Id, session.Id).Messages.Count);

        Assert.IsTrue(_historyStore.DeleteSession(_companion.Id, session.Id));
        Assert.AreEqual(0, _historyStore.GetMessagePage(_companion.Id, session.Id).Messages.Count);
        Assert.AreEqual(0, _historyStore.GetSessions(_companion.Id).Count);
    }

    [TestMethod]
    public void ChatHistory_PersistsAcrossStoreInstances()
    {
        var session = _historyStore.CreateSession(_companion.Id);
        _historyStore.SaveMessage(_companion.Id, session.Id, "user", "重启后仍然存在");

        var reopenedStore = new ChatHistoryStore(_databasePath);
        reopenedStore.Initialize();

        var page = reopenedStore.GetMessagePage(_companion.Id, session.Id);
        Assert.AreEqual(1, page.Messages.Count);
        Assert.AreEqual("重启后仍然存在", page.Messages[0].Content);
    }

    [TestMethod]
    public void Store_RejectsInvalidRolesAndUnboundedPageOrMessageSizes()
    {
        var session = _historyStore.CreateSession(_companion.Id);

        Assert.ThrowsException<ArgumentException>(() =>
            _historyStore.SaveMessage(_companion.Id, session.Id, "system", "not allowed"));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            _historyStore.GetMessagePage(_companion.Id, session.Id, pageSize: 101));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            _historyStore.GetMessagePage(_companion.Id, session.Id, beforeMessageId: 0));
        Assert.ThrowsException<ArgumentException>(() =>
            _historyStore.SaveMessage(
                _companion.Id,
                session.Id,
                "user",
                new string('x', ChatHistoryStore.MaximumMessageCharacters + 1)));
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
