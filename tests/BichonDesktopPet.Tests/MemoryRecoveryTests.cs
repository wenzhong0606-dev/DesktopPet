using System;
using System.IO;
using System.Linq;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class MemoryRecoveryTests
{
    [TestMethod]
    public void ObservationFailure_RollsBackAllCandidatesAndCursorAcrossReopen()
    {
        var directory = Path.Combine(Path.GetTempPath(), "DesktopPet.MemoryRecovery", Guid.NewGuid().ToString("N"));
        try
        {
            var path = Path.Combine(directory, "companions.db");
            var identities = new CompanionIdentityStore(path);
            identities.Initialize();
            var id = identities.EnsureCompanion(new AvatarProfile { Id = "a", Name = "A" }).Id;
            var store = new CompanionMemoryStore(path);
            store.Initialize();
            var history = new ChatHistoryStore(path);
            history.Initialize();
            var session = history.CreateSession(id);
            store.SetAutomaticEnabled(id, true);
            var first = history.SaveMessage(id, session.Id, "user", "我喜欢咖啡");
            Assert.AreEqual(1, store.ObserveUserMessage(id, first.Id));
            var next = history.SaveMessage(id, session.Id, "user", "我不喜欢咖啡，也喜欢奶茶");

            void Execute(string sql)
            {
                using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }

            // Inject failure after the candidate writes, at the observation cursor.
            Execute("""
                CREATE TRIGGER fail_memory_cursor BEFORE UPDATE ON companion_memory_observation
                BEGIN SELECT RAISE(ABORT,'injected cursor failure'); END;
                """);
            Assert.ThrowsException<SqliteException>(() => store.ObserveUserMessage(id, next.Id));
            store = new CompanionMemoryStore(path);
            Assert.AreEqual(1L, store.Count(id));
            Assert.AreEqual("用户喜欢咖啡", store.GetPage(id).Single().Content);
            Execute("DROP TRIGGER fail_memory_cursor;");
            store.Initialize();
            Assert.AreEqual(2, store.ObserveUserMessage(id, next.Id));
            Assert.AreEqual(2L, store.Count(id));
            Assert.AreEqual("用户不喜欢咖啡", store.GetPage(id).Single(m => m.Topic == "咖啡").Content);
            Assert.AreEqual("用户喜欢奶茶", store.GetPage(id).Single(m => m.Topic == "奶茶").Content);
            Assert.AreEqual(0, store.ObserveUserMessage(id, next.Id));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
