using System;
using System.IO;
using System.Linq;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BichonDesktopPet.Tests;

[TestClass]
public sealed class ShortTermIntegrationTests
{
    [TestMethod]
    public void ShortTermContext_ExpiresAndStaleFollowUpDoesNotOverwrite()
    {
        var directory=Path.Combine(Path.GetTempPath(),"DesktopPet.ShortTerm",Guid.NewGuid().ToString("N"));
        try
        {
            var path=Path.Combine(directory,"companions.db");
            var identities=new CompanionIdentityStore(path);
            identities.Initialize();
            var id=identities.EnsureCompanion(new AvatarProfile { Id="a",Name="A" }).Id;
            var store=new CompanionMemoryStore(path);
            store.Initialize();
            var history=new ChatHistoryStore(path);
            history.Initialize();
            var session=history.CreateSession(id);
            store.SetAutomaticEnabled(id,true);
            store.SetRetrievalEnabled(id,true);
            var state=history.SaveMessage(id,session.Id,"user","我今天有点累");
            Assert.AreEqual(1,store.ObserveUserMessage(id,state.Id));
            Assert.IsTrue(store.Retrieve(id,"我累吗").Any(m=>m.Topic=="状态·疲劳自述"));
            var preference=history.SaveMessage(id,session.Id,"user","我喜欢咖啡");
            Assert.AreEqual(1,store.ObserveUserMessage(id,preference.Id));
            using(var connection=new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var command=connection.CreateCommand();
                command.CommandText="UPDATE companion_memories SET expires_at_utc='2000-01-01T00:00:00.0000000+00:00' WHERE topic='状态·疲劳自述'; UPDATE chat_messages SET created_at_utc='2000-01-01T00:00:00.0000000+00:00' WHERE message_id=$message;";
                command.Parameters.AddWithValue("$message",preference.Id);
                command.ExecuteNonQuery();
            }
            Assert.IsFalse(store.Retrieve(id,"我累吗").Any());
            var correction=history.SaveMessage(id,session.Id,"user","现在不喜欢了");
            Assert.AreEqual(0,store.ObserveUserMessage(id,correction.Id));
            Assert.AreEqual("用户喜欢咖啡",store.GetPage(id).Single(m=>m.Topic=="咖啡").Content);
        }
        finally { if(Directory.Exists(directory))Directory.Delete(directory,true); }
    }
}
