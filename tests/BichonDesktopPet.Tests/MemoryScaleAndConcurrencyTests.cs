using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class MemoryScaleAndConcurrencyTests
{
    private string _directory = null!;
    private string _path = null!;
    private string _id = null!;
    private CompanionMemoryStore _store = null!;
    public TestContext TestContext { get; set; } = null!;

    [TestInitialize]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "DesktopPet.MemoryScale", Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_directory, "companions.db");
        var identities = new CompanionIdentityStore(_path);
        identities.Initialize();
        _id = identities.EnsureCompanion(new AvatarProfile { Id = "a", Name = "A" }).Id;
        _store = new CompanionMemoryStore(_path);
        _store.Initialize();
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_directory, true);

    [TestMethod]
    public void TenThousandRows_RetrieveOldMemoryAndReportLatency()
    {
        _store.Save(_id, "早期偏好", "早期记录");
        // Bulk fixture creation isolates retrieval cost from per-record write latency.
        using (var connection = new SqliteConnection($"Data Source={_path};Pooling=False"))
        {
            connection.Open();
            using var transaction = connection.BeginTransaction();
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                WITH RECURSIVE numbers(n) AS (
                    SELECT 1 UNION ALL SELECT n+1 FROM numbers WHERE n<10000
                )
                INSERT INTO companion_memories(companion_id,topic,content,updated_at_utc)
                SELECT $id,printf('测试主题%05d',n),'测试内容',$now FROM numbers;
                """;
            command.Parameters.AddWithValue("$id", _id);
            command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O"));
            command.ExecuteNonQuery();
            transaction.Commit();
        }
        _store = new CompanionMemoryStore(_path);
        _store.Initialize();
        Assert.AreEqual(10001L, _store.Count(_id));
        _store.SetRetrievalEnabled(_id, true);
        var timings = new double[10];
        for (int i = 0; i < timings.Length; i++)
        {
            var timer = Stopwatch.StartNew();
            var result = _store.Retrieve(_id, "早期偏好");
            timer.Stop();
            timings[i] = timer.Elapsed.TotalMilliseconds;
            Assert.AreEqual("早期记录", result.Single().Content);
        }
        Array.Sort(timings);
        TestContext.WriteLine($"10k retrieval median={(timings[4]+timings[5])/2:F2}ms max={timings[^1]:F2}ms");
        Assert.AreEqual(0, _store.Retrieve(_id, "今天天气如何").Count);
        Assert.AreEqual(1, _store.GetPage(_id, 10000).Count);
    }

    [TestMethod]
    public async Task ConcurrentReservations_ShareBudgetAndRespectRevocation()
    {
        _store.SetAutomaticEnabled(_id, true);
        _store.SetCloudEnhancedEnabled(_id, true);
        _store.SetRetrievalEnabled(_id, true);
        _store.SetSemanticEnabled(_id, true);
        var results = await Task.WhenAll(Enumerable.Range(0, 32).Select(i => Task.Run(() =>
            new CompanionMemoryStore(_path).TryReserveExtraRequest(_id, i % 2 == 0))));
        Assert.AreEqual(CompanionMemoryStore.DailyExtraRequestLimit, results.Count(x => x));
        var reopened = new CompanionMemoryStore(_path);
        reopened.Initialize();
        Assert.IsFalse(reopened.TryReserveExtraRequest(_id, true));
        Assert.IsFalse(reopened.TryReserveExtraRequest(_id, false));
        reopened.SetAutomaticEnabled(_id, false);
        reopened.SetRetrievalEnabled(_id, false);
        reopened.SetAutomaticEnabled(_id, true);
        reopened.SetRetrievalEnabled(_id, true);
        Assert.IsFalse(reopened.IsCloudEnhancedEnabled(_id));
        Assert.IsFalse(reopened.IsSemanticEnabled(_id));
    }
}
