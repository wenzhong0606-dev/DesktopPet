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
    public static readonly TimeSpan AutomaticPreferenceLifetime = TimeSpan.FromDays(180);
    private const int CandidateLimit = 48;
    private readonly TimeProvider _clock;
    private readonly string _connectionString;
    private const string Fields = "topic,content,updated_at_utc,category,source,source_message_id,confidence,important,archived,expires_at_utc,source_reply_id";

    public CompanionMemoryStore(string databasePath, TimeProvider? clock = null)
    {
        _clock = clock ?? TimeProvider.System;
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
            CREATE TABLE IF NOT EXISTS companion_memory_cloud_preferences (
                companion_id TEXT PRIMARY KEY NOT NULL REFERENCES companions(companion_id) ON DELETE CASCADE,
                enabled INTEGER NOT NULL CHECK(enabled IN(0,1))
            );
            CREATE TABLE IF NOT EXISTS companion_memory_advanced_preferences (
                companion_id TEXT PRIMARY KEY NOT NULL REFERENCES companions(companion_id) ON DELETE CASCADE,
                open_enabled INTEGER NOT NULL DEFAULT 0 CHECK(open_enabled IN(0,1)),
                semantic_enabled INTEGER NOT NULL DEFAULT 0 CHECK(semantic_enabled IN(0,1))
            );
            CREATE TABLE IF NOT EXISTS companion_memory_request_budget (
                companion_id TEXT PRIMARY KEY NOT NULL REFERENCES companions(companion_id) ON DELETE CASCADE,
                utc_day TEXT NOT NULL, used INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS companion_memory_observation (
                companion_id TEXT PRIMARY KEY NOT NULL,
                last_message_id INTEGER NOT NULL,
                FOREIGN KEY(companion_id) REFERENCES companions(companion_id) ON DELETE CASCADE
            );
            CREATE TABLE IF NOT EXISTS companion_memory_exchange_cursor (
                companion_id TEXT PRIMARY KEY NOT NULL,
                last_reply_id INTEGER NOT NULL,
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
            ("confidence", "TEXT NOT NULL DEFAULT 'user_confirmed'"),
            ("important", "INTEGER NOT NULL DEFAULT 0"),
            ("archived", "INTEGER NOT NULL DEFAULT 0"),
            ("expires_at_utc", "TEXT"),
            ("expiry_custom", "INTEGER NOT NULL DEFAULT 0"),
            ("source_reply_id", "INTEGER")
        })
        {
            if (columns.Contains(name)) continue;
            command.CommandText = $"ALTER TABLE companion_memories ADD COLUMN {name} {definition};";
            command.ExecuteNonQuery();
        }
        // A newly introduced archive column permits a non-destructive migration of
        // legacy automatic synonyms. Never silently merge two manual confirmations.
        if (!columns.Contains("archived"))
        {
            command.CommandText = """
                UPDATE companion_memories AS m SET archived=1
                WHERE m.source='automatic' AND EXISTS(
                    SELECT 1 FROM companion_memories AS other
                    WHERE other.companion_id=m.companion_id AND other.topic<>m.topic
                    AND memory_key(other.topic)=memory_key(m.topic)
                    AND (other.source='manual' OR
                         (other.source='automatic' AND
                          (COALESCE(other.source_message_id,0)>COALESCE(m.source_message_id,0) OR
                           (COALESCE(other.source_message_id,0)=COALESCE(m.source_message_id,0)
                            AND other.topic<m.topic)))));
                """;
            command.ExecuteNonQuery();
        }
        // Preserve ordering when upgrading from B2's existing automatic source IDs.
        command.CommandText = """
            INSERT INTO companion_memory_observation(companion_id,last_message_id)
            SELECT companion_id,MAX(source_message_id) FROM companion_memories
            WHERE source='automatic' AND source_message_id IS NOT NULL GROUP BY companion_id
            ON CONFLICT(companion_id) DO UPDATE SET last_message_id=MAX(last_message_id,excluded.last_message_id);
            """;
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public bool IsCloudEnhancedEnabled(string companionId) =>
        IsAutomaticEnabled(companionId) && ReadFlag(companionId,
            "SELECT enabled FROM companion_memory_cloud_preferences WHERE companion_id=$id;");
    public void SetCloudEnhancedEnabled(string companionId, bool enabled)
    {
        using var connection = Open();
        using var command = Command(connection, companionId, """
            INSERT INTO companion_memory_cloud_preferences(companion_id,enabled)
            VALUES($id,CASE WHEN $enabled=1 AND EXISTS(
                SELECT 1 FROM companion_memory_automatic_preferences WHERE companion_id=$id AND enabled=1)
                THEN 1 ELSE 0 END)
            ON CONFLICT(companion_id) DO UPDATE SET enabled=excluded.enabled;
            """);
        command.Parameters.AddWithValue("$enabled", enabled ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public bool IsOpenPreferenceEnabled(string companionId) =>
        IsAutomaticEnabled(companionId) && ReadFlag(companionId,
            "SELECT open_enabled FROM companion_memory_advanced_preferences WHERE companion_id=$id;");
    public bool IsSemanticEnabled(string companionId) =>
        IsRetrievalEnabled(companionId) && ReadFlag(companionId,
            "SELECT semantic_enabled FROM companion_memory_advanced_preferences WHERE companion_id=$id;");

    public void SetOpenPreferenceEnabled(string companionId, bool enabled) =>
        SetAdvanced(companionId,enabled,true);
    public void SetSemanticEnabled(string companionId, bool enabled) =>
        SetAdvanced(companionId,enabled,false);

    private void SetAdvanced(string companionId,bool enabled,bool open)
    {
        var field=open?"open_enabled":"semantic_enabled";
        var prerequisite=open?"companion_memory_automatic_preferences":"companion_memory_preferences";
        var flag=open?"enabled":"retrieval_enabled";
        using var connection=Open();
        using var command=Command(connection,companionId,$"""
            INSERT INTO companion_memory_advanced_preferences(companion_id,{field})
            VALUES($id,CASE WHEN $enabled=1 AND EXISTS(
                SELECT 1 FROM {prerequisite} WHERE companion_id=$id AND {flag}=1)
                THEN 1 ELSE 0 END)
            ON CONFLICT(companion_id) DO UPDATE SET {field}=excluded.{field};
            """);
        command.Parameters.AddWithValue("$enabled",enabled?1:0);
        command.ExecuteNonQuery();
    }

    public const int DailyExtraRequestLimit=20;
    /// <summary>Reserve an attempt before a billable request. Failed requests count too.</summary>
    public bool TryReserveExtraRequest(string companionId, bool semantic)
    {
        using var connection=Open();
        using var transaction=connection.BeginTransaction();
        using var command=Command(connection,companionId,semantic?"""
            INSERT INTO companion_memory_request_budget(companion_id,utc_day,used)
            SELECT $id,$day,1 WHERE EXISTS(
                SELECT 1 FROM companion_memory_advanced_preferences a
                JOIN companion_memory_preferences p ON p.companion_id=a.companion_id
                WHERE a.companion_id=$id AND a.semantic_enabled=1 AND p.retrieval_enabled=1)
            ON CONFLICT(companion_id) DO UPDATE SET utc_day=excluded.utc_day,
                used=CASE WHEN utc_day<excluded.utc_day THEN 1 ELSE used+1 END
            WHERE utc_day<excluded.utc_day OR (utc_day=excluded.utc_day AND used<$limit);
            """:"""
            INSERT INTO companion_memory_request_budget(companion_id,utc_day,used)
            SELECT $id,$day,1 WHERE EXISTS(
                SELECT 1 FROM companion_memory_cloud_preferences c
                JOIN companion_memory_automatic_preferences a ON a.companion_id=c.companion_id
                WHERE c.companion_id=$id AND c.enabled=1 AND a.enabled=1)
            ON CONFLICT(companion_id) DO UPDATE SET utc_day=excluded.utc_day,
                used=CASE WHEN utc_day<excluded.utc_day THEN 1 ELSE used+1 END
            WHERE utc_day<excluded.utc_day OR (utc_day=excluded.utc_day AND used<$limit);
            """);
        command.Transaction=transaction;
        command.Parameters.AddWithValue("$day",_clock.GetUtcNow().ToString("yyyy-MM-dd",CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$limit",DailyExtraRequestLimit);
        var reserved=command.ExecuteNonQuery()==1;
        transaction.Commit();
        return reserved;
    }

    public bool IsRetrievalEnabled(string companionId) => ReadFlag(companionId,
        "SELECT retrieval_enabled FROM companion_memory_preferences WHERE companion_id=$id;");
    public bool IsAutomaticEnabled(string companionId) => ReadFlag(companionId,
        "SELECT enabled FROM companion_memory_automatic_preferences WHERE companion_id=$id;");
    public void SetRetrievalEnabled(string companionId, bool enabled)
    {
        using var connection=Open();
        using var transaction=connection.BeginTransaction();
        using var command=Command(connection,companionId,"""
            INSERT INTO companion_memory_preferences(companion_id,retrieval_enabled) VALUES($id,$enabled)
            ON CONFLICT(companion_id) DO UPDATE SET retrieval_enabled=excluded.retrieval_enabled;
            UPDATE companion_memory_advanced_preferences SET semantic_enabled=0 WHERE companion_id=$id AND $enabled=0;
            """);
        command.Transaction=transaction;
        command.Parameters.AddWithValue("$enabled",enabled?1:0);
        command.ExecuteNonQuery();
        transaction.Commit();
    }
    public void SetAutomaticEnabled(string companionId, bool enabled)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = Command(connection, companionId, """
            INSERT INTO companion_memory_automatic_preferences(companion_id,enabled) VALUES($id,$enabled)
            ON CONFLICT(companion_id) DO UPDATE SET enabled=excluded.enabled;
            UPDATE companion_memory_cloud_preferences SET enabled=0 WHERE companion_id=$id AND $enabled=0;
            UPDATE companion_memory_advanced_preferences SET open_enabled=0 WHERE companion_id=$id AND $enabled=0;
            """);
        command.Transaction=transaction;
        command.Parameters.AddWithValue("$enabled", enabled ? 1 : 0);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

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
                updated_at_utc=excluded.updated_at_utc,source='manual',source_message_id=NULL,source_reply_id=NULL,
                confidence='user_confirmed',
                expires_at_utc=CASE WHEN expiry_custom=1 THEN expires_at_utc ELSE NULL END;
            """);
        AddMemoryParameters(command, topic, content);
        command.ExecuteNonQuery();
    }

    /// <summary>Read the persisted user message, never an assistant reply or old history scan.
    /// Newer explicit statements may replace automatic entries, but never manual confirmations.</summary>
    public int ObserveUserMessage(string companionId, long messageId, string? cloudEvidenceJson = null)
    {
        if (messageId <= 0 || !IsAutomaticEnabled(companionId)) return 0;
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var lookup = Command(connection, companionId, """
            SELECT m.content,m.created_at_utc,
                (SELECT p.content FROM chat_messages p WHERE p.session_id=m.session_id
                    AND p.role='user' AND p.message_id<m.message_id
                    AND julianday(m.created_at_utc)-julianday(p.created_at_utc) BETWEEN 0 AND 10.0/1440
                    ORDER BY p.message_id DESC LIMIT 1)
            FROM chat_messages m JOIN chat_sessions s ON s.session_id=m.session_id
            WHERE m.message_id=$message AND m.role='user' AND s.companion_id=$id
                AND EXISTS(SELECT 1 FROM companion_memory_automatic_preferences WHERE companion_id=$id AND enabled=1)
                AND $message > COALESCE((SELECT last_message_id FROM companion_memory_observation WHERE companion_id=$id),0);
            """);
        lookup.Transaction = transaction;
        lookup.Parameters.AddWithValue("$message", messageId);
        string message;
        string? previousUserText;
        DateTimeOffset recordedAt;
        using (var reader = lookup.ExecuteReader())
        {
            if (!reader.Read()) return 0;
            message = reader.GetString(0);
            recordedAt = DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture);
            previousUserText = reader.IsDBNull(2) ? null : reader.GetString(2);
        }
        var candidates = ExperienceMemoryExtractor.ExtractUserStatement(message, recordedAt);
        if (candidates.Count == 0) candidates = LocalMemoryExtractor.Extract(message);
        if (candidates.Count == 0) candidates = LocalMemoryExtractor.ExtractFollowUp(message, previousUserText);
        if (candidates.Count == 0 && ShortTermMemoryExtractor.Extract(message) is { } shortTerm)
            candidates = [shortTerm with { Content = $"{shortTerm.Content}（记录时间：{FormatUtc(recordedAt)}）" }];
        if (candidates.Count == 0 && CareFeedbackExtractor.Extract(message) is { } careFeedback)
            candidates = [careFeedback];
        var openVocabulary = false;
        if(candidates.Count==0)
        {
            using var consent=Command(connection,companionId,
                "SELECT open_enabled FROM companion_memory_advanced_preferences WHERE companion_id=$id;");
            consent.Transaction=transaction;
            if(consent.ExecuteScalar() is long enabled && enabled==1)
            {
                candidates=OpenPreferenceExtractor.Extract(message);
                openVocabulary=candidates.Count>0;
            }
        }
        var cloudAssisted = false;
        if (candidates.Count == 0 && cloudEvidenceJson is not null)
        {
            using var consent = Command(connection, companionId,
                "SELECT enabled FROM companion_memory_cloud_preferences WHERE companion_id=$id;");
            consent.Transaction = transaction;
            if (consent.ExecuteScalar() is long enabled && enabled == 1)
            {
                var evidence = BichonDesktopPet.Core.AI.CloudMemoryEvidence.Validate(message, cloudEvidenceJson);
                var selected = new List<MemoryCandidate>();
                foreach (var clause in evidence) selected.AddRange(LocalMemoryExtractor.Extract(clause));
                candidates = selected;
                cloudAssisted = candidates.Count > 0;
            }
        }
        var saved = 0;
        foreach (var candidate in candidates)
        {
            // A legacy alias can already exist. Preserve its displayed topic and
            // manual choices instead of creating a new synonymous record.
            using var match = Command(connection, companionId, $"""
                SELECT {Fields} FROM companion_memories WHERE companion_id=$id
                    AND memory_key(topic)=memory_key($topic)
                ORDER BY CASE WHEN source='manual' THEN 0 ELSE 1 END,
                    source_message_id DESC,updated_at_utc DESC,topic LIMIT 1;
                """);
            match.Transaction = transaction;
            match.Parameters.AddWithValue("$topic", candidate.Topic);
            var matches = ReadMemories(match);
            var existing = matches.Count == 0 ? null : matches[0];
            if (existing is { Source: "manual" } || existing is { Archived: true }) continue;
            var topic = existing?.Topic ?? candidate.Topic;
            var now = _clock.GetUtcNow();
            using var command = Command(connection, companionId, """
                INSERT INTO companion_memories(companion_id,topic,content,updated_at_utc,category,source,source_message_id,confidence,expires_at_utc)
                VALUES($id,$topic,$content,$now,$category,'automatic',$message,$confidence,$expiry)
                ON CONFLICT(companion_id,topic) DO UPDATE SET
                    content=excluded.content,updated_at_utc=excluded.updated_at_utc,
                    category=excluded.category,source_message_id=excluded.source_message_id,
                    confidence=excluded.confidence,
                    expires_at_utc=CASE WHEN companion_memories.expiry_custom=1 THEN companion_memories.expires_at_utc
                        ELSE excluded.expires_at_utc END
                WHERE companion_memories.source='automatic' AND companion_memories.archived=0
                    AND COALESCE(companion_memories.source_message_id,0)<excluded.source_message_id;
                """);
            command.Transaction = transaction;
            AddMemoryParameters(command, topic, candidate.Content);
            command.Parameters.AddWithValue("$category", candidate.Category);
            command.Parameters.AddWithValue("$message", messageId);
            command.Parameters.AddWithValue("$confidence", cloudAssisted ? "cloud_selected_literal" : openVocabulary ? "open_explicit_statement" : "explicit_statement");
            command.Parameters.AddWithValue("$expiry", candidate.Category == "用户经历"
                ? DBNull.Value : FormatUtc((ShortTermMemoryExtractor.Lifetime(candidate.Category) ?? CareFeedbackExtractor.Lifetime(candidate.Category)) is { } lifetime ? recordedAt + lifetime : now + AutomaticPreferenceLifetime));
            var changed = command.ExecuteNonQuery();
            // Reaffirmations refresh evidence and expiry but are not new or changed facts.
            if (existing is null || existing.Content != candidate.Content || !existing.IsAvailableAt(now))
                saved += changed;
        }
        // Advance even for an unrecognized or duplicate message, so a replay cannot
        // revive deleted entries or roll a preference back to older evidence.
        using var cursor = Command(connection, companionId, """
            INSERT INTO companion_memory_observation(companion_id,last_message_id) VALUES($id,$message)
            ON CONFLICT(companion_id) DO UPDATE SET last_message_id=MAX(last_message_id,excluded.last_message_id);
            """);
        cursor.Transaction = transaction;
        cursor.Parameters.AddWithValue("$message", messageId);
        cursor.ExecuteNonQuery();
        transaction.Commit();
        return saved;
    }

    /// <summary>Record only a recognized request paired with its persisted next reply.
    /// Never interpret assistant content as evidence of a real-world event.</summary>
    public int ObserveCompletedExchange(string companionId, long userMessageId, long replyMessageId)
    {
        if (userMessageId <= 0 || replyMessageId <= userMessageId || !IsAutomaticEnabled(companionId)) return 0;
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var lookup = Command(connection, companionId, """
            SELECT u.content,r.created_at_utc FROM chat_messages u
            JOIN chat_messages r ON r.session_id=u.session_id
            JOIN chat_sessions s ON s.session_id=u.session_id
            WHERE u.message_id=$user AND r.message_id=$reply AND u.role='user' AND r.role='assistant'
                AND s.companion_id=$id
                AND EXISTS(SELECT 1 FROM companion_memory_automatic_preferences WHERE companion_id=$id AND enabled=1)
                AND $reply>COALESCE((SELECT last_reply_id FROM companion_memory_exchange_cursor WHERE companion_id=$id),0)
                AND NOT EXISTS(SELECT 1 FROM chat_messages m WHERE m.session_id=u.session_id
                    AND m.message_id>u.message_id AND m.message_id<r.message_id);
            """);
        lookup.Transaction = transaction;
        lookup.Parameters.AddWithValue("$user", userMessageId);
        lookup.Parameters.AddWithValue("$reply", replyMessageId);
        MemoryCandidate? candidate;
        using (var reader = lookup.ExecuteReader())
        {
            if (!reader.Read()) return 0;
            candidate = ExperienceMemoryExtractor.ExtractExchange(reader.GetString(0),
                DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture));
        }
        var saved = 0;
        if (candidate is not null)
        {
            using var command = Command(connection, companionId, """
                INSERT INTO companion_memories(companion_id,topic,content,updated_at_utc,category,source,source_message_id,confidence,source_reply_id)
                VALUES($id,$topic,$content,$now,'共同经历','automatic',$user,'observed_exchange',$reply)
                ON CONFLICT(companion_id,topic) DO NOTHING;
                """);
            command.Transaction = transaction;
            AddMemoryParameters(command, candidate.Topic, candidate.Content);
            command.Parameters.AddWithValue("$user", userMessageId);
            command.Parameters.AddWithValue("$reply", replyMessageId);
            saved = command.ExecuteNonQuery();
        }
        using var cursor = Command(connection, companionId, """
            INSERT INTO companion_memory_exchange_cursor(companion_id,last_reply_id) VALUES($id,$reply)
            ON CONFLICT(companion_id) DO UPDATE SET last_reply_id=MAX(last_reply_id,excluded.last_reply_id);
            """);
        cursor.Transaction = transaction;
        cursor.Parameters.AddWithValue("$reply", replyMessageId);
        cursor.ExecuteNonQuery();
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

    public IReadOnlyList<CompanionMemory> Retrieve(string companionId, string query, string? previousUserQuery = null, string? semanticExpansion = null)
    {
        if (string.IsNullOrWhiteSpace(query) || !IsRetrievalEnabled(companionId)) return [];
        // Carry context only for narrow follow-up expressions, not unrelated new questions.
        if (MemoryVocabulary.IsFollowUp(query) && !string.IsNullOrWhiteSpace(previousUserQuery))
            query += "\n" + previousUserQuery[..Math.Min(previousUserQuery.Length, 2000)];
        if (!IsSemanticEnabled(companionId)) semanticExpansion = null;
        var now = _clock.GetUtcNow();
        using var connection = Open();
        using var command = Command(connection, companionId, $"""
            WITH ranked AS (
                SELECT {Fields},MAX(memory_score(topic,category,$query),
                    CASE WHEN source='automatic' AND category IN ('饮食','音乐','兴趣','色彩')
                        AND confidence IN ('explicit_statement','cloud_selected_literal','open_explicit_statement')
                        THEN memory_score(topic,category,$expansion)* 0.65 ELSE 0 END) AS relevance,
                    ROW_NUMBER() OVER(PARTITION BY memory_key(topic)
                        ORDER BY CASE WHEN source='manual' THEN 0 ELSE 1 END,
                        source_message_id DESC,updated_at_utc DESC,topic) AS representative
                FROM companion_memories
                WHERE companion_id=$id
            )
            SELECT {Fields} FROM ranked WHERE representative=1 AND relevance>0 AND archived=0
                AND (expires_at_utc IS NULL OR expires_at_utc>$now)
            ORDER BY relevance + CASE WHEN important=1 THEN 15 ELSE 0 END DESC,
                updated_at_utc DESC,topic LIMIT $limit;
            """);
        command.Parameters.AddWithValue("$query", query);
        command.Parameters.AddWithValue("$expansion", semanticExpansion ?? "");
        command.Parameters.AddWithValue("$now", FormatUtc(now));
        command.Parameters.AddWithValue("$limit", CandidateLimit);
        return MemoryContextBudget.Select(ReadMemories(command), now: now);
    }

    public bool SetImportant(string companionId, string topic, bool important) =>
        SetMetadata(companionId, topic, "UPDATE companion_memories SET important=$value WHERE companion_id=$id AND topic=$topic;",
            important ? 1 : 0);

    public bool SetArchived(string companionId, string topic, bool archived) =>
        SetMetadata(companionId, topic, """
            UPDATE companion_memories SET archived=$value,
                expires_at_utc=CASE WHEN $value=0 THEN NULL ELSE expires_at_utc END,
                expiry_custom=CASE WHEN $value=0 THEN 1 ELSE expiry_custom END
            WHERE companion_id=$id AND topic=$topic;
            """, archived ? 1 : 0);

    public bool SetExpiry(string companionId, string topic, DateTimeOffset? expiry) =>
        SetMetadata(companionId, topic,
            "UPDATE companion_memories SET expires_at_utc=$value,expiry_custom=1 WHERE companion_id=$id AND topic=$topic;",
            expiry is { } value ? FormatUtc(value) : DBNull.Value);

    private bool SetMetadata(string companionId, string topic, string sql, object value)
    {
        using var connection = Open();
        using var command = Command(connection, companionId, sql);
        command.Parameters.AddWithValue("$topic", topic);
        command.Parameters.AddWithValue("$value", value);
        return command.ExecuteNonQuery() > 0;
    }

    private static IReadOnlyList<CompanionMemory> ReadMemories(SqliteCommand command)
    {
        using var reader = command.ExecuteReader();
        var memories = new List<CompanionMemory>();
        while (reader.Read())
            memories.Add(new CompanionMemory(reader.GetString(0), reader.GetString(1),
                DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
                reader.GetString(3), reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetInt64(5), reader.GetString(6), reader.GetInt64(7) != 0, reader.GetInt64(8) != 0,
                reader.IsDBNull(9) ? null : DateTimeOffset.Parse(reader.GetString(9), CultureInfo.InvariantCulture),
                reader.IsDBNull(10) ? null : reader.GetInt64(10)));
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

    private void AddMemoryParameters(SqliteCommand command, string topic, string content)
    {
        command.Parameters.AddWithValue("$topic", topic);
        command.Parameters.AddWithValue("$content", content);
        command.Parameters.AddWithValue("$now", FormatUtc(_clock.GetUtcNow()));
    }

    private static string FormatUtc(DateTimeOffset timestamp) =>
        timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        try
        {
            connection.Open();
            connection.CreateFunction<string, string>("memory_key", MemoryVocabulary.CanonicalTopic, isDeterministic: true);
            connection.CreateFunction<string, string, string, int>("memory_score", MemoryVocabulary.Relevance, isDeterministic: true);
            return connection;
        }
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
