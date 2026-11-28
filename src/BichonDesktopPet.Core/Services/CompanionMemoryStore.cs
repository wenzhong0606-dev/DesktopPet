using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BichonDesktopPet.Core.Models;
using Microsoft.Data.Sqlite;

namespace BichonDesktopPet.Core.Services;

/// <summary>Explicitly authored local memories, isolated by stable companion ID.</summary>
public sealed class CompanionMemoryStore
{
    public const int MaximumMemories = 200;
    public const int MaximumTopicCharacters = 40;
    public const int MaximumContentCharacters = 200;
    public const int MaximumRetrievedMemories = 4;
    private readonly string _connectionString;

    public CompanionMemoryStore(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(databasePath),
            Mode = SqliteOpenMode.ReadWrite,
            ForeignKeys = true,
            Pooling = false
        }.ToString();
    }

    public void Initialize()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS companion_memory_preferences (
                companion_id TEXT PRIMARY KEY NOT NULL,
                retrieval_enabled INTEGER NOT NULL CHECK(retrieval_enabled IN (0,1)),
                FOREIGN KEY(companion_id) REFERENCES companions(companion_id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS companion_memories (
                companion_id TEXT NOT NULL,
                topic TEXT COLLATE NOCASE NOT NULL,
                content TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL,
                PRIMARY KEY(companion_id, topic),
                FOREIGN KEY(companion_id) REFERENCES companions(companion_id) ON DELETE CASCADE
            );
            """;
        command.ExecuteNonQuery();
    }

    public bool IsRetrievalEnabled(string companionId)
    {
        using var connection = Open();
        using var command = Command(connection, companionId,
            "SELECT retrieval_enabled FROM companion_memory_preferences WHERE companion_id=$id;");
        return command.ExecuteScalar() is long value && value == 1;
    }

    public void SetRetrievalEnabled(string companionId, bool enabled)
    {
        using var connection = Open();
        using var command = Command(connection, companionId, """
            INSERT INTO companion_memory_preferences(companion_id,retrieval_enabled) VALUES($id,$enabled)
            ON CONFLICT(companion_id) DO UPDATE SET retrieval_enabled=excluded.retrieval_enabled;
            """);
        command.Parameters.AddWithValue("$enabled", enabled ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public void Save(string companionId, string topic, string content)
    {
        topic = (topic ?? "").Trim();
        content = (content ?? "").Trim();
        if (topic.Length is < 2 or > MaximumTopicCharacters || topic.Any(char.IsControl))
            throw new ArgumentException("记忆主题必须为 2–40 个字符，且不能包含控制字符。");
        if (content.Length is < 1 or > MaximumContentCharacters)
            throw new ArgumentException("记忆内容必须为 1–200 个字符。");

        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = Command(connection, companionId, """
            INSERT INTO companion_memories(companion_id,topic,content,updated_at_utc)
            SELECT $id,$topic,$content,$now
            WHERE EXISTS(SELECT 1 FROM companion_memories WHERE companion_id=$id AND topic=$topic)
               OR (SELECT COUNT(*) FROM companion_memories WHERE companion_id=$id) < $cap
            ON CONFLICT(companion_id,topic) DO UPDATE SET content=excluded.content,updated_at_utc=excluded.updated_at_utc;
            """);
        command.Transaction = transaction;
        command.Parameters.AddWithValue("$topic", topic);
        command.Parameters.AddWithValue("$content", content);
        command.Parameters.AddWithValue("$cap", MaximumMemories);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        if (command.ExecuteNonQuery() != 1)
            throw new InvalidOperationException("已达到 200 条记忆上限，请先删除不再需要的记忆。");
        transaction.Commit();
    }

    public IReadOnlyList<CompanionMemory> GetAll(string companionId)
    {
        using var connection = Open();
        using var command = Command(connection, companionId, """
            SELECT topic,content,updated_at_utc FROM companion_memories
            WHERE companion_id=$id ORDER BY updated_at_utc DESC,topic LIMIT $cap;
            """);
        command.Parameters.AddWithValue("$cap", MaximumMemories);
        using var reader = command.ExecuteReader();
        var memories = new List<CompanionMemory>();
        while (reader.Read())
            memories.Add(new CompanionMemory(reader.GetString(0), reader.GetString(1),
                DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture)));
        return memories;
    }

    public IReadOnlyList<CompanionMemory> Retrieve(string companionId, string query)
    {
        if (string.IsNullOrWhiteSpace(query) || !IsRetrievalEnabled(companionId))
            return Array.Empty<CompanionMemory>();
        return GetAll(companionId)
            .Where(memory => query.Contains(memory.Topic, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(memory => memory.Topic.Length)
            .ThenByDescending(memory => memory.UpdatedAtUtc)
            .Take(MaximumRetrievedMemories).ToArray();
    }

    public bool Delete(string companionId, string topic)
    {
        using var connection = Open();
        using var command = Command(connection, companionId,
            "DELETE FROM companion_memories WHERE companion_id=$id AND topic=$topic;");
        command.Parameters.AddWithValue("$topic", topic);
        return command.ExecuteNonQuery() > 0;
    }

    public int Clear(string companionId)
    {
        using var connection = Open();
        using var command = Command(connection, companionId,
            "DELETE FROM companion_memories WHERE companion_id=$id;");
        return command.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        try { connection.Open(); return connection; }
        catch { connection.Dispose(); throw; }
    }

    private static SqliteCommand Command(SqliteConnection connection, string companionId, string sql)
    {
        if (string.IsNullOrWhiteSpace(companionId))
            throw new ArgumentException("Companion ID is required.", nameof(companionId));
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", companionId);
        return command;
    }
}
