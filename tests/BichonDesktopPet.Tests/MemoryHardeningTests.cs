using System;
using System.IO;
using System.Linq;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class MemoryHardeningTests
{
    [DataTestMethod]
    [DataRow("我朋友喜欢咖啡")]
    [DataRow("如果我喜欢咖啡")]
    [DataRow("我喜欢咖啡吗？")]
    [DataRow("今天突然想喝咖啡")]
    [DataRow("他说：我喜欢咖啡")]
    [DataRow("我喜欢咖啡，我的密码是测试")]
    [DataRow("我喜欢咖啡，但不喜欢咖啡")]
    [DataRow("我以前喜欢咖啡")]
    public void AmbiguousOrSensitiveMessagesAreNotStored(string input)
        => Assert.AreEqual(0, LocalMemoryExtractor.Extract(input).Count, input);

    [DataTestMethod]
    [DataRow("我一直喜欢游泳", "游泳", "用户喜欢游泳")]
    [DataRow("我不太喜欢奶茶", "奶茶", "用户不太喜欢奶茶")]
    [DataRow("我以前喜欢咖啡，现在不喜欢了", "咖啡", "用户不喜欢咖啡")]
    public void ExplicitEvidencePreservesTopicAndPolarity(string input, string topic, string content)
    {
        var memory = LocalMemoryExtractor.Extract(input).Single();
        Assert.AreEqual(topic, memory.Topic);
        Assert.AreEqual(content, memory.Content);
    }

    [TestMethod]
    public void ThousandMemoriesRemainPageableAndIsolatedAfterReopen()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DesktopPet.MemoryHardening", Guid.NewGuid().ToString("N"));
        try
        {
            var path = Path.Combine(directory, "companions.db");
            var identities = new CompanionIdentityStore(path);
            identities.Initialize();
            var first = identities.EnsureCompanion(new AvatarProfile { Id = "a", Name = "A" });
            var second = identities.EnsureCompanion(new AvatarProfile { Id = "b", Name = "B" });
            var store = new CompanionMemoryStore(path);
            store.Initialize();
            store.Save(first.Id, "早期偏好", "第一角色");
            store.Save(second.Id, "早期偏好", "第二角色");
            for (int i = 0; i < 1000; i++)
                store.Save(first.Id, $"测试主题{i:D4}", "测试内容");
            store = new CompanionMemoryStore(path);
            store.Initialize();
            Assert.AreEqual(1001L, store.Count(first.Id));
            var all = Enumerable.Range(0, 34).SelectMany(i => store.GetPage(first.Id, i * 30)).ToArray();
            Assert.AreEqual(1001, all.Length);
            Assert.AreEqual(1001, all.Select(m => m.Topic).Distinct().Count());
            store.SetRetrievalEnabled(first.Id, true);
            Assert.AreEqual("第一角色", store.Retrieve(first.Id, "早期偏好").Single().Content);
            Assert.AreEqual(0, store.Retrieve(second.Id, "早期偏好").Count);
            store.SetRetrievalEnabled(second.Id, true);
            Assert.AreEqual("第二角色", store.Retrieve(second.Id, "早期偏好").Single().Content);
            Assert.AreEqual(0, store.Retrieve(first.Id, "今天天气如何").Count);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
