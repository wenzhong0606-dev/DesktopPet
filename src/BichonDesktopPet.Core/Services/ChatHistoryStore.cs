using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BichonDesktopPet.Core.Models;
using Microsoft.Data.Sqlite;

namespace BichonDesktopPet.Core.Services;

/// <summary>
/// Stores chat sessions and messages in the existing local companion database.
/// Message pages use an indexed keyset cursor, so older rows are never loaded
/// unless the user explicitly requests another page.
/// </summary>
public sealed class ChatHistoryStore
{
    public const int DefaultPageSize = 30;
    public const int MaximumPageSize = 100;
    public const int DefaultSessionLimit = 100;
    public const int MaximumMessageCharacters = 20000;
    private const int MaximumTitleCharacters = 40;
    private const string NewSessionTitle = "新对话";

    private readonly string _databasePath;
    private readonly string _connectionString;
    private bool _initialized;

    public ChatHistoryStore(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("A chat history database path is required.", nameof(databasePath));
        }

        _databasePath = Path.GetFullPath(databasePath);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true,
            Pooling = false
        }.ToString();
    }

    public string DatabasePath => _databasePath;

    /// <summary>
    /// Adds chat tables to the existing companions.db without changing existing tables or data.
    /// CompanionIdentityStore.Initialize must run first.
    /// </summary>
    public void Initialize()
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("The chat history database must have a parent directory.");
        }

        Directory.CreateDirectory(directory);

        using var connection = OpenConnection();
        using (var check = connection.CreateCommand())
        {
            check.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'companions';";
            if (check.ExecuteScalar() is null)
            {
                throw new InvalidOperationException("Initialize CompanionIdentityStore before ChatHistoryStore.");
            }
        }

        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode = WAL;
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS chat_sessions (
                session_id TEXT NOT NULL PRIMARY KEY,
                companion_id TEXT NOT NULL,
                title TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL,
                FOREIGN KEY (companion_id) REFERENCES companions(companion_id)
                    ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ix_chat_sessions_companion_updated
                ON chat_sessions(companion_id, updated_at_utc DESC, session_id DESC);

            CREATE TABLE IF NOT EXISTS chat_messages (
                message_id INTEGER PRIMARY KEY AUTOINCREMENT,
                session_id TEXT NOT NULL,
                role TEXT NOT NULL CHECK(role IN ('user', 'assistant')),
                content TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                FOREIGN KEY (session_id) REFERENCES chat_sessions(session_id)
                    ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ix_chat_messages_session_id
                ON chat_messages(session_id, message_id DESC);
            """;
        command.ExecuteNonQuery();
        _initialized = true;
    }

    public ChatSession CreateSession(string companionId, DateTimeOffset? now = null)
    {
        EnsureInitialized();
        ValidateIdentifier(companionId, nameof(companionId));

        var id = Guid.NewGuid().ToString("N");
        var timestampValue = now ?? DateTimeOffset.UtcNow;
        var timestamp = FormatUtc(timestampValue);

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO chat_sessions (
                session_id, companion_id, title, created_at_utc, updated_at_utc
            )
            VALUES ($sessionId, $companionId, $title, $createdAtUtc, $updatedAtUtc);
            """;
        command.Parameters.AddWithValue("$sessionId", id);
        command.Parameters.AddWithValue("$companionId", companionId);
        command.Parameters.AddWithValue("$title", NewSessionTitle);
        command.Parameters.AddWithValue("$createdAtUtc", timestamp);
        command.Parameters.AddWithValue("$updatedAtUtc", timestamp);
        command.ExecuteNonQuery();

        return new ChatSession
        {
            Id = id,
            CompanionId = companionId,
            Title = NewSessionTitle,
            CreatedAtUtc = timestampValue,
            UpdatedAtUtc = timestampValue,
            MessageCount = 0
        };
    }

    public IReadOnlyList<ChatSession> GetSessions(
        string companionId,
        int limit = DefaultSessionLimit)
    {
        EnsureInitialized();
        ValidateIdentifier(companionId, nameof(companionId));

        if (limit is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Session limit must be between 1 and 500.");
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                s.session_id,
                s.companion_id,
                s.title,
                s.created_at_utc,
                s.updated_at_utc,
                (SELECT COUNT(*) FROM chat_messages m WHERE m.session_id = s.session_id)
            FROM chat_sessions s
            WHERE s.companion_id = $companionId
            ORDER BY s.updated_at_utc DESC, s.session_id DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$companionId", companionId);
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var sessions = new List<ChatSession>();
        while (reader.Read())
        {
            sessions.Add(new ChatSession
            {
                Id = reader.GetString(0),
                CompanionId = reader.GetString(1),
                Title = reader.GetString(2),
                CreatedAtUtc = ParseUtc(reader.GetString(3)),
                UpdatedAtUtc = ParseUtc(reader.GetString(4)),
                MessageCount = reader.GetInt64(5)
            });
        }

        return sessions;
    }

    public ChatHistoryMessage SaveMessage(
        string companionId,
        string sessionId,
        string role,
        string content,
        DateTimeOffset? now = null)
    {
        EnsureInitialized();
        ValidateIdentifier(companionId, nameof(companionId));
        ValidateIdentifier(sessionId, nameof(sessionId));

        var normalizedRole = role?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalizedRole is not ("user" or "assistant"))
        {
            throw new ArgumentException("Only user and assistant messages can be stored.", nameof(role));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Message content cannot be empty.", nameof(content));
        }

        if (content.Length > MaximumMessageCharacters)
        {
            throw new ArgumentException(
                $"Message content cannot exceed {MaximumMessageCharacters} characters.",
                nameof(content));
        }

        var timestampValue = now ?? DateTimeOffset.UtcNow;
        var timestamp = FormatUtc(timestampValue);
        var title = CreateTitle(content);
        long messageId;

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        using (var verify = connection.CreateCommand())
        {
            verify.Transaction = transaction;
            verify.CommandText = """
                SELECT 1 FROM chat_sessions
                WHERE session_id = $sessionId AND companion_id = $companionId;
                """;
            verify.Parameters.AddWithValue("$sessionId", sessionId);
            verify.Parameters.AddWithValue("$companionId", companionId);
            if (verify.ExecuteScalar() is null)
            {
                throw new KeyNotFoundException("The chat session does not belong to this companion or no longer exists.");
            }
        }

        using (var insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO chat_messages (session_id, role, content, created_at_utc)
                VALUES ($sessionId, $role, $content, $createdAtUtc);
                SELECT last_insert_rowid();
                """;
            insert.Parameters.AddWithValue("$sessionId", sessionId);
            insert.Parameters.AddWithValue("$role", normalizedRole);
            insert.Parameters.AddWithValue("$content", content);
            insert.Parameters.AddWithValue("$createdAtUtc", timestamp);
            messageId = Convert.ToInt64(insert.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        using (var update = connection.CreateCommand())
        {
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE chat_sessions
                SET title = CASE
                        WHEN $role = 'user'
                         AND title = $newSessionTitle
                         AND NOT EXISTS (
                             SELECT 1 FROM chat_messages
                             WHERE session_id = $sessionId
                               AND role = 'user'
                               AND message_id < $messageId
                         )
                        THEN $title
                        ELSE title
                    END,
                    updated_at_utc = $updatedAtUtc
                WHERE session_id = $sessionId AND companion_id = $companionId;
                """;
            update.Parameters.AddWithValue("$role", normalizedRole);
            update.Parameters.AddWithValue("$newSessionTitle", NewSessionTitle);
            update.Parameters.AddWithValue("$sessionId", sessionId);
            update.Parameters.AddWithValue("$messageId", messageId);
            update.Parameters.AddWithValue("$title", title);
            update.Parameters.AddWithValue("$updatedAtUtc", timestamp);
            update.Parameters.AddWithValue("$companionId", companionId);
            update.ExecuteNonQuery();
        }

        transaction.Commit();

        return new ChatHistoryMessage
        {
            Id = messageId,
            SessionId = sessionId,
            Role = normalizedRole,
            Content = content,
            CreatedAtUtc = timestampValue
        };
    }

    /// <summary>
    /// Returns the newest page, or a page strictly older than beforeMessageId.
    /// The database query is bounded to pageSize + 1 rows and uses the indexed message ID cursor.
    /// Returned messages are in chronological display order.
    /// </summary>
    public ChatHistoryPage GetMessagePage(
        string companionId,
        string sessionId,
        int pageSize = DefaultPageSize,
        long? beforeMessageId = null)
    {
        EnsureInitialized();
        ValidateIdentifier(companionId, nameof(companionId));
        ValidateIdentifier(sessionId, nameof(sessionId));

        if (pageSize is < 1 or > MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                $"Page size must be between 1 and {MaximumPageSize}.");
        }

        if (beforeMessageId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(beforeMessageId));
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT m.message_id, m.session_id, m.role, m.content, m.created_at_utc
            FROM chat_messages m
            WHERE m.session_id = $sessionId
              AND EXISTS (
                  SELECT 1 FROM chat_sessions s
                  WHERE s.session_id = m.session_id
                    AND s.companion_id = $companionId
              )
              AND ($beforeMessageId IS NULL OR m.message_id < $beforeMessageId)
            ORDER BY m.message_id DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$companionId", companionId);
        command.Parameters.AddWithValue(
            "$beforeMessageId",
            beforeMessageId.HasValue ? beforeMessageId.Value : DBNull.Value);
        command.Parameters.AddWithValue("$limit", pageSize + 1);

        using var reader = command.ExecuteReader();
        var messages = new List<ChatHistoryMessage>();
        while (reader.Read())
        {
            messages.Add(new ChatHistoryMessage
            {
                Id = reader.GetInt64(0),
                SessionId = reader.GetString(1),
                Role = reader.GetString(2),
                Content = reader.GetString(3),
                CreatedAtUtc = ParseUtc(reader.GetString(4))
            });
        }

        var hasMore = messages.Count > pageSize;
        if (hasMore)
        {
            messages.RemoveAt(messages.Count - 1);
        }

        messages.Reverse();
        return new ChatHistoryPage
        {
            Messages = messages,
            HasMore = hasMore,
            NextBeforeMessageId = messages.Count == 0 ? null : messages[0].Id
        };
    }

    public bool DeleteSession(string companionId, string sessionId)
    {
        EnsureInitialized();
        ValidateIdentifier(companionId, nameof(companionId));
        ValidateIdentifier(sessionId, nameof(sessionId));

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM chat_sessions
            WHERE session_id = $sessionId AND companion_id = $companionId;
            """;
        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$companionId", companionId);
        return command.ExecuteNonQuery() > 0;
    }

    public int DeleteAllSessions(string companionId)
    {
        EnsureInitialized();
        ValidateIdentifier(companionId, nameof(companionId));

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM chat_sessions WHERE companion_id = $companionId;";
        command.Parameters.AddWithValue("$companionId", companionId);
        return command.ExecuteNonQuery();
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Initialize the chat history store before using it.");
        }
    }

    private static void ValidateIdentifier(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A non-empty identifier is required.", parameterName);
        }
    }

    private static string CreateTitle(string content)
    {
        var normalized = content
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();

        if (normalized.Length > MaximumTitleCharacters)
        {
            normalized = normalized[..MaximumTitleCharacters];
        }

        return normalized.Length == 0 ? NewSessionTitle : normalized;
    }

    private static string FormatUtc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseUtc(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.None);
}
