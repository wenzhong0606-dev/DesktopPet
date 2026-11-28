using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class MemoryContextBudgetTests
{
    [TestMethod]
    public void Select_AdaptsCountToActualUtf8JsonBytesAndEscaping()
    {
        var now = DateTimeOffset.UtcNow;
        var shortEntries = Enumerable.Range(0, 20)
            .Select(i => new CompanionMemory($"主题{i}", "短内容", now)).ToArray();
        var shortSelected = MemoryContextBudget.Select(shortEntries);
        Assert.AreEqual(MemoryContextBudget.MaximumEntries, shortSelected.Count);
        var longEntries = Enumerable.Range(0, 20)
            .Select(i => new CompanionMemory($"主题{i}", new string('汉', 180) + new string('"', 20), now)).ToArray();
        var longSelected = MemoryContextBudget.Select(longEntries);
        Assert.IsTrue(longSelected.Count < shortSelected.Count);
        Assert.IsTrue(longSelected.Count > 0);
        var json = MemoryContextBudget.BuildJson(longEntries);
        Assert.IsTrue(Encoding.UTF8.GetByteCount(json) <= MemoryContextBudget.MaximumPayloadBytes);
        using var document = JsonDocument.Parse(json);
        Assert.AreEqual(longSelected.Count, document.RootElement.GetArrayLength());
    }

    [TestMethod]
    public void Select_ExcludesExpiredArchivedAndSynonymousDuplicates()
    {
        var now = DateTimeOffset.UtcNow;
        CompanionMemory[] entries =
        [
            new("看书", "喜欢看书", now),
            new("阅读", "旧记录", now),
            new("咖啡", "过期", now, ExpiresAtUtc: now.AddDays(-1)),
            new("茶", "归档", now, Archived: true),
            new("音乐", "正常", now)
        ];
        var selected = MemoryContextBudget.Select(entries, now: now);
        CollectionAssert.AreEqual(new[] { "看书", "音乐" }, selected.Select(m => m.Topic).ToArray());
        Assert.AreEqual("[]", MemoryContextBudget.BuildJson(entries.Skip(2).Take(2)));
    }

    [TestMethod]
    public void Select_SkipsOversizeEntryWithoutDiscardingSmallerMatches()
    {
        var now = DateTimeOffset.UtcNow;
        var entries = new[] { new CompanionMemory("主题甲", new string('汉', 200), now),
            new CompanionMemory("主题乙", "短", now) };
        var selected = MemoryContextBudget.Select(entries, 120, now);
        Assert.AreEqual("主题乙", selected.Single().Topic);
    }
}
