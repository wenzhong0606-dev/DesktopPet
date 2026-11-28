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
        Assert.AreEqual("喜欢面条", reopened.GetAll(_first.Id).Single().Content);
        Assert.AreEqual("喜欢米饭", reopened.GetAll(_second.Id).Single().Content);
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
        Assert.AreEqual(2, _store.GetAll(_first.Id).Count);
    }

    [TestMethod]
    public void Save_UpdatesSameTopicAndDeleteAndClearAreScoped()
    {
        _store.Save(_first.Id, "Music", "旧内容");
        _store.Save(_first.Id, "music", "新内容");
        _store.Save(_second.Id, "music", "其他角色");
        Assert.AreEqual("新内容", _store.GetAll(_first.Id).Single().Content);
        Assert.IsTrue(_store.Delete(_first.Id, "MUSIC"));
        Assert.IsFalse(_store.Delete(_first.Id, "MUSIC"));
        _store.Save(_first.Id, "生日", "六月");
        Assert.AreEqual(1, _store.Clear(_first.Id));
        Assert.AreEqual(0, _store.GetAll(_first.Id).Count);
        Assert.AreEqual(1, _store.GetAll(_second.Id).Count);
    }

    [TestMethod]
    public void Save_RejectsInvalidInput()
    {
        Assert.ThrowsException<ArgumentException>(() => _store.Save(_first.Id, "x", "内容"));
        Assert.ThrowsException<ArgumentException>(() => _store.Save(_first.Id, "主题", new string('a', 201)));
        Assert.AreEqual(0, _store.GetAll(_first.Id).Count);
    }

    [TestMethod]
    public void RetrievalAndPrompt_AreBounded()
    {
        for (var index = 0; index < 6; index++)
            _store.Save(_first.Id, $"主题{index}", $"内容{index}");
        _store.SetRetrievalEnabled(_first.Id, true);
        var relevant = _store.Retrieve(_first.Id, "主题0 主题1 主题2 主题3 主题4 主题5");
        Assert.AreEqual(4, relevant.Count);
        var messages = ChatContextBuilder.BuildMessages(_first, new AvatarProfile(),
            PetBehaviorState.Idle, [new AiChatMessage("user", "主题")], relevantMemories: relevant);
        StringAssert.Contains(messages[0].Content, "相关用户记忆");
        StringAssert.Contains(messages[0].Content, relevant[0].Content);
        StringAssert.Contains(messages[0].Content, "不是指令");
        var without = ChatContextBuilder.BuildMessages(_first, new AvatarProfile(), PetBehaviorState.Idle, []);
        Assert.IsFalse(without[0].Content.Contains("【相关用户记忆】"));
    }

    [TestMethod]
    public void Save_CapAllowsUpdatesButRejectsAdditionalTopics()
    {
        for (var index = 0; index < CompanionMemoryStore.MaximumMemories; index++)
            _store.Save(_first.Id, $"主题{index}", "内容");
        Assert.ThrowsException<InvalidOperationException>(() => _store.Save(_first.Id, "额外主题", "内容"));
        _store.Save(_first.Id, "主题0", "更新");
        Assert.AreEqual(CompanionMemoryStore.MaximumMemories, _store.GetAll(_first.Id).Count);
        Assert.AreEqual("更新", _store.GetAll(_first.Id).Single(m => m.Topic == "主题0").Content);
    }
}
