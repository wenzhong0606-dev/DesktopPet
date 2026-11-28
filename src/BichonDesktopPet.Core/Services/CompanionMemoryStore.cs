using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BichonDesktopPet.Core.Models;
using Microsoft.Data.Sqlite;

namespace BichonDesktopPet.Core.Services;

public sealed class CompanionMemoryStore
{
    public const int DefaultPageSize = 30;
    public const int MaximumPageSize = 100;
    public const int MaximumTopicCharacters = 40;
    public const int MaximumContentCharacters = 200;
    public const int MaximumRetrievedMemories = 4;
    private readonly string _connectionString;
    private const string Fields = "topic,content,updated_at_utc,category,source,source_message_id,confidence";

    public CompanionMemoryStore(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(databasePath), Mode = SqliteOpenMode.ReadWrite,
            ForeignKeys = true, Pooling = false
        }.ToString();
    }

    public void Initialize()
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS companion_memory_preferences (
                companion_id TEXT PRIMARY KEY NOT NULL,
                retrieval_enabled INTEGER NOT NULL CHECK(retrieval_enabled IN (0,1)),
                FOREIGN KEY(companion_id) REFERENCES companions(companion_id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS companion_memory_automatic_preferences (
                companion_id TEXT PRIMARY KEY NOT NULL,
                enabled INTEGER NOT NULL CHECK(enabled IN (0,1)),
                FOREIGN KEY(companion_id) REFERENCES companions(companion_id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS companion_memories (
                companion_id TEXT NOT NULL,
                topic TEXT COLLATE NOCASE NOT NULL,
                content TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL,
                PRIMARY KEY(companion_id,topic),
                FOREIGN KEY(companion_id) REFERENCES companions(companion_id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_companion_memories_updated
                ON companion_memories(companion_id,updated_at_utc DESC,topic);
            """;
        command.ExecuteNonQuery();

        command.CommandText = "PRAGMA table_info(companion_memories);";
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var reader = command.ExecuteReader())
            while (reader.Read()) columns.Add(reader.GetString(1));
        foreach (var (name, definition) in new (string, string)[]
        {
            ("category", "TEXT NOT NULL DEFAULT '未分类'"),
            ("source", "TEXT NOT NULL DEFAULT 'manual'"),
            ("source_message_id", "INTEGER"),
            ("confidence", "TEXT NOT NULL DEFAULT 'user_confirmed'")
        })
        {
            if (columns.Contains(name)) continue;
            command.CommandText = $"ALTER TABLE companion_memories ADD COLUMN {name} {definition};";
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    public bool IsRetrievalEnabled(string companionId) => ReadFlag(companionId,
        "SELECT retrieval_enabled FROM companion_memory_preferences WHERE companion_id=$id;");
    public bool IsAutomaticEnabled(string companionId) => ReadFlag(companionId,
        "SELECT enabled FROM companion_memory_automatic_preferences WHERE companion_id=$id;");
    public void SetRetrievalEnabled(string companionId, bool enabled) => WriteFlag(companionId, enabled, """
        INSERT INTO companion_memory_preferences(companion_id,retrieval_enabled) VALUES($id,$enabled)
        ON CONFLICT(companion_id) DO UPDATE SET retrieval_enabled=excluded.retrieval_enabled;
        """);
    public void SetAutomaticEnabled(string companionId, bool enabled) => WriteFlag(companionId, enabled, """
        INSERT INTO companion_memory_automatic_preferences(companion_id,enabled) VALUES($id,$enabled)
        ON CONFLICT(companion_id) DO UPDATE SET enabled=excluded.enabled;
        """);

    private bool ReadFlag(string id, string sql)
    {
        using var connection = Open();
        using var command = Command(connection, id, sql);
        return command.ExecuteScalar() is long value && value == 1;
    }

    private void WriteFlag(string id, bool enabled, string sql)
    {
        using var connection = Open();
        using var command = Command(connection, id, sql);
        command.Parameters.AddWithValue("$enabled", enabled ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public void Save(string companionId, string topic, string content)
    {
        topic = (topic ?? "").Trim();
        content = (content ?? "").Trim();
        if (topic.Length is < 1 or > MaximumTopicCharacters || topic.Any(char.IsControl))
            throw new ArgumentException("记忆主题必须为 1–40 个字符，且不能包含控制字符。");
        if (content.Length is < 1 or > MaximumContentCharacters)
            throw new ArgumentException("记忆内容必须为 1–200 个字符。");
        using var connection = Open();
        using var command = Command(connection, companionId, """
            INSERT INTO companion_memories(companion_id,topic,content,updated_at_utc,source,confidence)
            VALUES($id,$topic,$content,$now,'manual','user_confirmed')
            ON CONFLICT(companion_id,topic) DO UPDATE SET content=excluded.content,
                updated_at_utc=excluded.updated_at_utc,source='manual',source_message_id=NULL,
                confidence='user_confirmed';
            """);
        AddMemoryParameters(command, topic, content);
        command.ExecuteNonQuery();
    }

    /// <summary>Read the persisted user message, never an assistant reply or old history scan.
    /// A same-topic conflict is left unchanged until explicit user correction or a later policy.</summary>
    public int ObserveUserMessage(string companionId, long messageId)
    {
        if (messageId <= 0 || !IsAutomaticEnabled(companionId)) return 0;
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var lookup = Command(connection, companionId, """
            SELECT m.content FROM chat_messages m JOIN chat_sessions s ON s.session_id=m.session_id
            WHERE m.message_id=$message AND m.role='user' AND s.companion_id=$id
                AND EXISTS(SELECT 1 FROM companion_memory_automatic_preferences WHERE companion_id=$id AND enabled=1);
            """);
        lookup.Transaction = transaction;
        lookup.Parameters.AddWithValue("$message", messageId);
        var candidates = LocalMemoryExtractor.Extract(lookup.ExecuteScalar() as string);
        var saved = 0;
        foreach (var candidate in candidates)
        {
            using var command = Command(connection, companionId, """
                INSERT INTO companion_memories(companion_id,topic,content,updated_at_utc,category,source,source_message_id,confidence)
                VALUES($id,$topic,$content,$now,$category,'automatic',$message,'explicit_statement')
                ON CONFLICT(companion_id,topic) DO NOTHING;
                """);
            command.Transaction = transaction;
            AddMemoryParameters(command, candidate.Topic, candidate.Content);
            command.Parameters.AddWithValue("$category", candidate.Category);
            command.Parameters.AddWithValue("$message", messageId);
            saved += command.ExecuteNonQuery();
        }
        transaction.Commit();
        return saved;
    }

    public long Count(string companionId)
    {
        using var connection = Open();
        using var command = Command(connection, companionId,
            "SELECT COUNT(*) FROM companion_memories WHERE companion_id=$id;");
        return (long)(command.ExecuteScalar() ?? 0L);
    }

    public IReadOnlyList<CompanionMemory> GetPage(string companionId, int offset = 0, int limit = DefaultPageSize)
    {
        if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset));
        if (limit is < 1 or > MaximumPageSize) throw new ArgumentOutOfRangeException(nameof(limit));
        using var connection = Open();
        using var command = Command(connection, companionId,
            $"SELECT {Fields} FROM companion_memories WHERE companion_id=$id ORDER BY updated_at_utc DESC,topic LIMIT $limit OFFSET $offset;");
        command.Parameters.AddWithValue("$limit", limit);
        command.Parameters.AddWithValue("$offset", offset);
        return ReadMemories(command);
    }

    public IReadOnlyList<CompanionMemory> Retrieve(string companionId, string query)
    {
        if (string.IsNullOrWhiteSpace(query) || !IsRetrievalEnabled(companionId)) return [];
        // Small, deterministic category hints; semantic retrieval is a later increment.
        var categoryHint = query.Contains("吃什么", StringComparison.Ordinal) ||
            query.Contains("喝什么", StringComparison.Ordinal) ||
            query.Contains("喝点什么", StringComparison.Ordinal) ||
            query.Contains("吃点什么", StringComparison.Ordinal) ? "饮食" :
            query.Contains("听什么音乐", StringComparison.Ordinal) ||
            query.Contains("推荐音乐", StringComparison.Ordinal) ? "音乐" : "";
        using var connection = Open();
        using var command = Command(connection, companionId, $"""
            SELECT {Fields} FROM companion_memories WHERE companion_id=$id AND
                (instr(lower($query),lower(topic))>0 OR
                 (category<>'未分类' AND (instr($query,category)>0 OR category=$hint)))
            ORDER BY CASE WHEN instr(lower($query),lower(topic))>0 THEN 0 ELSE 1 END,
                length(topic) DESC,updated_at_utc DESC,topic LIMIT $limit;
            """);
        command.Parameters.AddWithValue("$query", query);
        command.Parameters.AddWithValue("$hint", categoryHint);
        command.Parameters.AddWithValue("$limit", MaximumRetrievedMemories);
        return ReadMemories(command);
    }

    private static IReadOnlyList<CompanionMemory> ReadMemories(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        var memories = new List<CompanionMemory>();
        while (reader.Read())
            memories.Add(new CompanionMemory(reader.GetString(0), reader.GetString(1),
                DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
                reader.GetString(3), reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetInt64(5), reader.GetString(6)));
        return memories;
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

    private static void AddMemoryParameters(SqliteCommand command, string topic, string content)
    {
        command.Parameters.AddWithValue("$topic", topic);
        command.Parameters.AddWithValue("$content", content);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        try { connection.Open(); return connection; }
        catch { connection.Dispose(); throw; }
    }

    private static SqliteCommand Command(SqliteConnection connection, string companionId, string sql)
    {
        if (string.IsNullOrWhiteSpace(companionId)) throw new ArgumentException("Companion ID is required.", nameof(companionId));
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", companionId);
        return command;
    }
}
