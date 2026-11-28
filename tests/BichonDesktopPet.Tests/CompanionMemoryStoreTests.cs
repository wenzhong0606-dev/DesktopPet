using System;
using System.IO;
using System.Linq;
using BichonDesktopPet.Core.AI;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class CompanionMemoryStoreTests
{
    private string _directory = null!;
    private string _path = null!;
    private CompanionMemoryStore _store = null!;
    private CompanionIdentity _first = null!, _second = null!;

    [TestInitialize]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "DesktopPet.MemoryTests", Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_directory, "companions.db");
        var identities = new CompanionIdentityStore(_path);
        identities.Initialize();
        _first = identities.EnsureCompanion(new AvatarProfile { Id = "a", Name = "A" });
        _second = identities.EnsureCompanion(new AvatarProfile { Id = "b", Name = "B" });
        _store = new CompanionMemoryStore(_path);
        _store.Initialize();
    }

    [TestCleanup]
    public void CleanUp() => Directory.Delete(_directory, true);

    [TestMethod]
    public void Save_IsLocalAndIsolatedAndPersistsAcrossReopen()
    {
        _store.Save(_first.Id, "饮食", "喜欢面条");
        _store.Save(_second.Id, "饮食", "喜欢米饭");
        var reopened = new CompanionMemoryStore(_path);
        reopened.Initialize();
        Assert.AreEqual("喜欢面条", reopened.GetPage(_first.Id).Single().Content);
        Assert.AreEqual("喜欢米饭", reopened.GetPage(_second.Id).Single().Content);
        Assert.IsFalse(reopened.IsRetrievalEnabled(_first.Id));
        Assert.AreEqual(0, reopened.Retrieve(_first.Id, "饮食").Count);
    }

    [TestMethod]
    public void Retrieve_OptInAndTopicMatchingAndPauseAreIndependentPerCompanion()
    {
        _store.Save(_first.Id, "饮食", "喜欢面条");
        _store.Save(_first.Id, "音乐", "喜欢爵士");
        _store.Save(_second.Id, "饮食", "喜欢米饭");
        _store.SetRetrievalEnabled(_first.Id, true);
        Assert.AreEqual("喜欢面条", _store.Retrieve(_first.Id, "我的饮食偏好是什么").Single().Content);
        Assert.AreEqual(0, _store.Retrieve(_first.Id, "天气怎么样").Count);
        Assert.AreEqual(0, _store.Retrieve(_second.Id, "饮食").Count);
        _store.SetRetrievalEnabled(_first.Id, false);
        Assert.AreEqual(0, _store.Retrieve(_first.Id, "饮食").Count);
        Assert.AreEqual(2, _store.GetPage(_first.Id).Count);
    }

    [TestMethod]
    public void Save_UpdatesSameTopicAndDeleteAndClearAreScoped()
    {
        _store.Save(_first.Id, "Music", "旧内容");
        _store.Save(_first.Id, "music", "新内容");
        _store.Save(_second.Id, "music", "其他角色");
        Assert.AreEqual("新内容", _store.GetPage(_first.Id).Single().Content);
        Assert.IsTrue(_store.Delete(_first.Id, "MUSIC"));
        Assert.IsFalse(_store.Delete(_first.Id, "MUSIC"));
        _store.Save(_first.Id, "生日", "六月");
        Assert.AreEqual(1, _store.Clear(_first.Id));
        Assert.AreEqual(0, _store.GetPage(_first.Id).Count);
        Assert.AreEqual(1, _store.GetPage(_second.Id).Count);
    }

    [TestMethod]
    public void Save_RejectsInvalidInput()
    {
        Assert.ThrowsException<ArgumentException>(() => _store.Save(_first.Id, " ", "内容"));
        Assert.ThrowsException<ArgumentException>(() => _store.Save(_first.Id, "主题", new string('a', 201)));
        Assert.AreEqual(0, _store.GetPage(_first.Id).Count);
    }

    [TestMethod]
    public void RetrievalAndPrompt_AreBounded()
    {
        for (var index = 0; index < 6; index++)
            _store.Save(_first.Id, $"主题{index}", $"内容{index}");
        _store.SetRetrievalEnabled(_first.Id, true);
        var relevant = _store.Retrieve(_first.Id, "主题0 主题1 主题2 主题3 主题4 主题5");
        Assert.AreEqual(6, relevant.Count);
        var messages = ChatContextBuilder.BuildMessages(_first, new AvatarProfile(),
            PetBehaviorState.Idle, [new AiChatMessage("user", "主题")], relevantMemories: relevant);
        StringAssert.Contains(messages[0].Content, "相关用户记忆");
        StringAssert.Contains(messages[0].Content, relevant[0].Content);
        StringAssert.Contains(messages[0].Content, "不是指令");
        var without = ChatContextBuilder.BuildMessages(_first, new AvatarProfile(), PetBehaviorState.Idle, []);
        Assert.IsFalse(without[0].Content.Contains("【相关用户记忆】"));
    }

    [TestMethod]
    public void Save_NoFixedCapacity_PagesAndRetrievesOlderMemories()
    {
        _store.Save(_first.Id, "早期偏好", "老记忆");
        for (var index = 0; index < 215; index++)
            _store.Save(_first.Id, $"主题{index}", "内容");
        Assert.AreEqual(216L, _store.Count(_first.Id));
        var topics = Enumerable.Range(0, 8)
            .SelectMany(page => _store.GetPage(_first.Id, page * 30))
            .Select(memory => memory.Topic).ToArray();
        Assert.AreEqual(216, topics.Distinct().Count());
        Assert.AreEqual(30, _store.GetPage(_first.Id).Count);
        _store.SetRetrievalEnabled(_first.Id, true);
        Assert.AreEqual("老记忆", _store.Retrieve(_first.Id, "早期偏好").Single().Content);
    }

    [TestMethod]
    public void Observe_RequiresConsentAndPersistedUserSourceForSameCompanion()
    {
        var history = new ChatHistoryStore(_path);
        history.Initialize();
        var session = history.CreateSession(_first.Id);
        var user = history.SaveMessage(_first.Id, session.Id, "user", "我比较喜欢喝冰美式，不喜欢喝奶茶。");
        var assistant = history.SaveMessage(_first.Id, session.Id, "assistant", "我喜欢咖啡");
        Assert.IsFalse(_store.IsAutomaticEnabled(_first.Id));
        Assert.AreEqual(0, _store.ObserveUserMessage(_first.Id, user.Id));
        _store.SetAutomaticEnabled(_first.Id, true);
        _store.SetAutomaticEnabled(_second.Id, true);
        Assert.AreEqual(0, _store.ObserveUserMessage(_second.Id, user.Id));
        Assert.AreEqual(0, _store.ObserveUserMessage(_first.Id, assistant.Id));
        Assert.AreEqual(2, _store.ObserveUserMessage(_first.Id, user.Id));
        Assert.AreEqual(0, _store.ObserveUserMessage(_first.Id, user.Id));
        Assert.AreEqual(0, _store.GetPage(_second.Id).Count);
        var saved = _store.GetPage(_first.Id).Single(m => m.Topic == "冰美式");
        Assert.AreEqual("用户喜欢冰美式", saved.Content);
        Assert.AreEqual("饮食", saved.Category);
        Assert.AreEqual("automatic", saved.Source);
        Assert.AreEqual(user.Id, saved.SourceMessageId.GetValueOrDefault());
        Assert.AreEqual("explicit_statement", saved.Confidence);
        Assert.IsFalse(_store.IsRetrievalEnabled(_first.Id));
        Assert.AreEqual(0, _store.Retrieve(_first.Id, "今天喝点什么").Count);
        _store.SetRetrievalEnabled(_first.Id, true);
        Assert.AreEqual(2, _store.Retrieve(_first.Id, "今天喝点什么").Count);
        _store.SetAutomaticEnabled(_first.Id, false);
        var later = history.SaveMessage(_first.Id, session.Id, "user", "我喜欢火锅");
        Assert.AreEqual(0, _store.ObserveUserMessage(_first.Id, later.Id));
        Assert.AreEqual(2L, _store.Count(_first.Id));
        var reopened = new CompanionMemoryStore(_path);
        reopened.Initialize();
        Assert.IsFalse(reopened.IsAutomaticEnabled(_first.Id));
        Assert.IsTrue(reopened.IsAutomaticEnabled(_second.Id));
    }

    [TestMethod]
    public void Observe_ProtectsManualMemoryAndUpdatesNewerAutomaticPreference()
    {
        var history = new ChatHistoryStore(_path);
        history.Initialize();
        var session = history.CreateSession(_first.Id);
        _store.Save(_first.Id, "咖啡", "不喜欢咖啡");
        _store.SetAutomaticEnabled(_first.Id, true);
        var first = history.SaveMessage(_first.Id, session.Id, "user", "我喜欢咖啡");
        Assert.AreEqual(0, _store.ObserveUserMessage(_first.Id, first.Id));
        Assert.AreEqual("不喜欢咖啡", _store.GetPage(_first.Id).Single().Content);
        var second = history.SaveMessage(_first.Id, session.Id, "user", "我喜欢看书");
        Assert.AreEqual(1, _store.ObserveUserMessage(_first.Id, second.Id));
        var third = history.SaveMessage(_first.Id, session.Id, "user", "我不喜欢看书");
        Assert.AreEqual(1, _store.ObserveUserMessage(_first.Id, third.Id));
        var automatic = _store.GetPage(_first.Id).Single(m => m.Topic == "看书");
        Assert.AreEqual("用户不喜欢看书", automatic.Content);
        Assert.AreEqual(third.Id, automatic.SourceMessageId.GetValueOrDefault());
        Assert.AreEqual(0, _store.ObserveUserMessage(_first.Id, second.Id));
        Assert.AreEqual("用户不喜欢看书", _store.GetPage(_first.Id).Single(m => m.Topic == "看书").Content);
        _store.Save(_first.Id, "看书", "现在不喜欢看书");
        var updated = _store.GetPage(_first.Id).Single(m => m.Topic == "看书");
        Assert.AreEqual("manual", updated.Source);
        Assert.IsNull(updated.SourceMessageId);
        Assert.AreEqual("现在不喜欢看书", updated.Content);
    }

    [TestMethod]
    public void Initialize_MigratesLegacySchemaWithoutLosingEntries()
    {
        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_path};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                DROP TABLE companion_memories;
                CREATE TABLE companion_memories(
                  companion_id TEXT NOT NULL,topic TEXT COLLATE NOCASE NOT NULL,content TEXT NOT NULL,
                  updated_at_utc TEXT NOT NULL,PRIMARY KEY(companion_id,topic),
                  FOREIGN KEY(companion_id) REFERENCES companions(companion_id) ON DELETE CASCADE);
                INSERT INTO companion_memories VALUES($id,'旧记忆','原始内容','2026-11-28T10:00:00.0000000+00:00');
                """;
            command.Parameters.AddWithValue("$id", _first.Id);
            command.ExecuteNonQuery();
        }
        _store.Initialize();
        _store.Initialize();
        var memory = _store.GetPage(_first.Id).Single();
        Assert.AreEqual("原始内容", memory.Content);
        Assert.AreEqual("manual", memory.Source);
        Assert.AreEqual("user_confirmed", memory.Confidence);
        Assert.IsNull(memory.SourceMessageId);
    }
    [TestMethod]
    public void Observe_DuplicateAdvancesCursorAndDeletionCannotBeUndoneByReplay()
    {
        var history = new ChatHistoryStore(_path);
        history.Initialize();
        var session = history.CreateSession(_first.Id);
        _store.SetAutomaticEnabled(_first.Id, true);
        var first = history.SaveMessage(_first.Id, session.Id, "user", "我喜欢咖啡");
        var duplicate = history.SaveMessage(_first.Id, session.Id, "user", "我喜欢咖啡");
        Assert.AreEqual(1, _store.ObserveUserMessage(_first.Id, first.Id));
        Assert.AreEqual(0, _store.ObserveUserMessage(_first.Id, duplicate.Id));
        Assert.IsTrue(_store.Delete(_first.Id, "咖啡"));
        var reopened = new CompanionMemoryStore(_path);
        reopened.Initialize();
        Assert.AreEqual(0, reopened.ObserveUserMessage(_first.Id, first.Id));
        Assert.AreEqual(0, reopened.ObserveUserMessage(_first.Id, duplicate.Id));
        Assert.AreEqual(0L, reopened.Count(_first.Id));
        var later = history.SaveMessage(_first.Id, session.Id, "user", "我现在喜欢咖啡");
        Assert.AreEqual(1, reopened.ObserveUserMessage(_first.Id, later.Id));
    }

    [TestMethod]
    public void Observe_ExplicitChangeAcrossSessionsIsPersistedAndRetrieved()
    {
        var history = new ChatHistoryStore(_path);
        history.Initialize();
        _store.SetAutomaticEnabled(_first.Id, true);
        _store.SetRetrievalEnabled(_first.Id, true);
        var session = history.CreateSession(_first.Id);
        var first = history.SaveMessage(_first.Id, session.Id, "user", "我喜欢咖啡");
        Assert.AreEqual(1, _store.ObserveUserMessage(_first.Id, first.Id));
        var nextSession = history.CreateSession(_first.Id);
        var changed = history.SaveMessage(_first.Id, nextSession.Id, "user", "我以前喜欢咖啡，现在不喜欢了");
        Assert.AreEqual(1, _store.ObserveUserMessage(_first.Id, changed.Id));
        var reopened = new CompanionMemoryStore(_path);
        reopened.Initialize();
        var saved = reopened.Retrieve(_first.Id, "喝点什么").Single();
        Assert.AreEqual("用户不喜欢咖啡", saved.Content);
        Assert.AreEqual(changed.Id, saved.SourceMessageId.GetValueOrDefault());
        Assert.AreEqual(1L, reopened.Count(_first.Id));
    }

    [TestMethod]
    public void Observe_SynonymsShareOneAutomaticPreferenceAndManualAliasIsProtected()
    {
        var history = new ChatHistoryStore(_path);
        history.Initialize();
        var session = history.CreateSession(_first.Id);
        _store.SetAutomaticEnabled(_first.Id, true);
        _store.SetRetrievalEnabled(_first.Id, true);
        var first = history.SaveMessage(_first.Id, session.Id, "user", "我喜欢阅读");
        var second = history.SaveMessage(_first.Id, session.Id, "user", "我现在不喜欢读书");
        Assert.AreEqual(1, _store.ObserveUserMessage(_first.Id, first.Id));
        Assert.AreEqual(1, _store.ObserveUserMessage(_first.Id, second.Id));
        Assert.AreEqual(1L, _store.Count(_first.Id));
        Assert.AreEqual("用户不喜欢看书", _store.Retrieve(_first.Id, "阅读").Single().Content);
        _store.Save(_first.Id, "绘画", "手动确认：喜欢绘画");
        var third = history.SaveMessage(_first.Id, session.Id, "user", "我不喜欢画画");
        Assert.AreEqual(0, _store.ObserveUserMessage(_first.Id, third.Id));
        Assert.AreEqual("手动确认：喜欢绘画", _store.Retrieve(_first.Id, "画画").Single().Content);
        Assert.AreEqual(2L, _store.Count(_first.Id));
    }

    [TestMethod]
    public void Initialize_ArchivesLegacyAutomaticAliasesWithoutDeletingManualRecords()
    {
        using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_path};Pooling=False"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                DROP TABLE companion_memories;
                CREATE TABLE companion_memories(
                    companion_id TEXT NOT NULL,topic TEXT COLLATE NOCASE NOT NULL,content TEXT NOT NULL,
                    updated_at_utc TEXT NOT NULL,category TEXT NOT NULL,source TEXT NOT NULL,
                    source_message_id INTEGER,confidence TEXT NOT NULL,PRIMARY KEY(companion_id,topic));
                INSERT INTO companion_memories VALUES
                    ($id,'阅读','用户喜欢阅读','2026-11-28T10:00:00.0000000+00:00','兴趣','automatic',1,'explicit_statement'),
                    ($id,'看书','用户不喜欢看书','2026-11-28T11:00:00.0000000+00:00','兴趣','automatic',2,'explicit_statement'),
                    ($id,'绘画','手动确认','2026-11-28T10:00:00.0000000+00:00','兴趣','manual',NULL,'user_confirmed'),
                    ($id,'画画','自动记录','2026-11-28T12:00:00.0000000+00:00','兴趣','automatic',3,'explicit_statement');
                """;
            command.Parameters.AddWithValue("$id", _first.Id);
            command.ExecuteNonQuery();
        }
        _store.Initialize();
        _store.Initialize();
        var page = _store.GetPage(_first.Id);
        Assert.AreEqual(4, page.Count);
        Assert.IsTrue(page.Single(m => m.Topic == "阅读").Archived);
        Assert.IsTrue(page.Single(m => m.Topic == "画画").Archived);
        Assert.IsFalse(page.Single(m => m.Topic == "绘画").Archived);
        _store.SetRetrievalEnabled(_first.Id, true);
        Assert.AreEqual("用户不喜欢看书", _store.Retrieve(_first.Id, "阅读").Single().Content);
        Assert.AreEqual("手动确认", _store.Retrieve(_first.Id, "画画").Single().Content);
        _store.SetArchived(_first.Id, "看书", true);
        Assert.AreEqual(0, _store.Retrieve(_first.Id, "阅读").Count);
    }

    [TestMethod]
    public void Retrieve_ExpiryAndArchiveAreReversibleAndScoped()
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 11, 28, 10, 0, 0, TimeSpan.Zero));
        var store = new CompanionMemoryStore(_path, clock);
        store.Initialize();
        store.Save(_first.Id, "咖啡", "喜欢咖啡");
        store.Save(_second.Id, "咖啡", "不喜欢咖啡");
        store.SetRetrievalEnabled(_first.Id, true);
        store.SetRetrievalEnabled(_second.Id, true);
        store.SetExpiry(_first.Id, "咖啡", clock.Now.AddDays(1));
        Assert.AreEqual(1, store.Retrieve(_first.Id, "咖啡").Count);
        clock.Now = clock.Now.AddDays(1);
        Assert.AreEqual(0, store.Retrieve(_first.Id, "咖啡").Count);
        Assert.AreEqual(1L, store.Count(_first.Id));
        Assert.AreEqual(1, store.Retrieve(_second.Id, "咖啡").Count);
        Assert.IsTrue(store.SetArchived(_first.Id, "咖啡", false));
        Assert.AreEqual(1, store.Retrieve(_first.Id, "咖啡").Count);
        Assert.IsNull(store.GetPage(_first.Id).Single().ExpiresAtUtc);
        Assert.IsTrue(store.SetArchived(_first.Id, "咖啡", true));
        var reopened = new CompanionMemoryStore(_path, clock);
        reopened.Initialize();
        Assert.AreEqual(0, reopened.Retrieve(_first.Id, "咖啡").Count);
        Assert.IsTrue(reopened.GetPage(_first.Id).Single().Archived);
    }

    [TestMethod]
    public void Observe_ReaffirmationRefreshesDefaultLifetimeButNotUserDefinedExpiryOrArchive()
    {
        var clock = new MutableClock(new DateTimeOffset(2026, 11, 28, 10, 0, 0, TimeSpan.Zero));
        var store = new CompanionMemoryStore(_path, clock);
        store.Initialize();
        var history = new ChatHistoryStore(_path);
        history.Initialize();
        var session = history.CreateSession(_first.Id);
        store.SetAutomaticEnabled(_first.Id, true);
        store.SetRetrievalEnabled(_first.Id, true);
        long Add(string message) => history.SaveMessage(_first.Id, session.Id, "user", message).Id;
        Assert.AreEqual(1, store.ObserveUserMessage(_first.Id, Add("我喜欢咖啡")));
        Assert.AreEqual(clock.Now.AddDays(180), store.GetPage(_first.Id).Single().ExpiresAtUtc.GetValueOrDefault());
        clock.Now = clock.Now.AddDays(20);
        Assert.AreEqual(0, store.ObserveUserMessage(_first.Id, Add("我喜欢咖啡")));
        Assert.AreEqual(clock.Now.AddDays(180), store.GetPage(_first.Id).Single().ExpiresAtUtc.GetValueOrDefault());
        var customExpiry = clock.Now.AddDays(2);
        store.SetExpiry(_first.Id, "咖啡", customExpiry);
        Assert.AreEqual(1, store.ObserveUserMessage(_first.Id, Add("我不喜欢咖啡")));
        Assert.AreEqual(customExpiry, store.GetPage(_first.Id).Single().ExpiresAtUtc.GetValueOrDefault());
        store.SetArchived(_first.Id, "咖啡", true);
        Assert.AreEqual(0, store.ObserveUserMessage(_first.Id, Add("我喜欢咖啡")));
        Assert.AreEqual(0, store.Retrieve(_first.Id, "咖啡").Count);
        Assert.AreEqual("用户不喜欢咖啡", store.GetPage(_first.Id).Single().Content);
    }

    [TestMethod]
    public void Retrieve_ImportanceRanksRelevantItemsButNeverInjectsUnrelatedMemory()
    {
        _store.Save(_first.Id, "主题甲", "甲");
        _store.Save(_first.Id, "主题乙", "乙");
        _store.Save(_first.Id, "不相关", "不相关");
        _store.SetImportant(_first.Id, "主题甲", true);
        _store.SetImportant(_first.Id, "不相关", true);
        _store.SetRetrievalEnabled(_first.Id, true);
        var matches = _store.Retrieve(_first.Id, "主题甲 主题乙");
        Assert.AreEqual(2, matches.Count);
        Assert.AreEqual("主题甲", matches[0].Topic);
        Assert.IsTrue(matches[0].Important);
        Assert.AreEqual(0, _store.Retrieve(_first.Id, "天气怎么样").Count);
    }

    private sealed class MutableClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
