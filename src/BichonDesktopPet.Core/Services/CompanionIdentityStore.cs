using System.Globalization;
using System.Text.Json;
using BichonDesktopPet.Core.Models;
using Microsoft.Data.Sqlite;

namespace BichonDesktopPet.Core.Services;

/// <summary>
/// Stores stable companion identities separately from the image/avatar catalog.
/// The database is local to the current Windows user and never requires a cloud account.
/// </summary>
public sealed class CompanionIdentityStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _databasePath;
    private readonly string _connectionString;
    private bool _initialized;

    public CompanionIdentityStore(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("A companion database path is required.", nameof(databasePath));
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

    public void Initialize()
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("The companion database must have a parent directory.");
        }

        Directory.CreateDirectory(directory);

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode = WAL;
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS companions (
                companion_id TEXT NOT NULL PRIMARY KEY,
                avatar_id TEXT NOT NULL UNIQUE,
                display_name TEXT NOT NULL,
                core_personality_json TEXT NOT NULL,
                evolving_personality_json TEXT NOT NULL DEFAULT '{}',
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL,
                is_archived INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS interaction_events (
                event_id TEXT NOT NULL PRIMARY KEY,
                companion_id TEXT NOT NULL,
                event_type TEXT NOT NULL,
                payload_json TEXT NOT NULL,
                occurred_at_utc TEXT NOT NULL,
                FOREIGN KEY (companion_id) REFERENCES companions(companion_id)
                    ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ix_interaction_events_companion_time
                ON interaction_events(companion_id, occurred_at_utc DESC);
            """;
        command.ExecuteNonQuery();
        _initialized = true;
    }

    /// <summary>
    /// Creates one stable companion for an existing avatar, or refreshes its
    /// display name and initial persona snapshot without touching evolving data.
    /// </summary>
    public CompanionIdentity EnsureCompanion(AvatarProfile avatar, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(avatar);
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(avatar.Id))
        {
            throw new ArgumentException("An avatar ID is required.", nameof(avatar));
        }

        var timestamp = FormatUtc(now ?? DateTimeOffset.UtcNow);
        var corePersonalityJson = JsonSerializer.Serialize(
            avatar.Persona ?? new PersonaProfile(),
            JsonOptions);

        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        string? companionId;
        using (var find = connection.CreateCommand())
        {
            find.Transaction = transaction;
            find.CommandText = "SELECT companion_id FROM companions WHERE avatar_id = $avatarId;";
            find.Parameters.AddWithValue("$avatarId", avatar.Id);
            companionId = find.ExecuteScalar() as string;
        }

        if (companionId is null)
        {
            companionId = Guid.NewGuid().ToString("N");
            using var insert = connection.CreateCommand();
            insert.Transaction = transaction;
            insert.CommandText = """
                INSERT INTO companions (
                    companion_id, avatar_id, display_name, core_personality_json,
                    evolving_personality_json, created_at_utc, updated_at_utc
                )
                VALUES (
                    $companionId, $avatarId, $displayName, $corePersonalityJson,
                    '{}', $createdAtUtc, $updatedAtUtc
                );
                """;
            insert.Parameters.AddWithValue("$companionId", companionId);
            insert.Parameters.AddWithValue("$avatarId", avatar.Id);
            insert.Parameters.AddWithValue("$displayName", avatar.Name);
            insert.Parameters.AddWithValue("$corePersonalityJson", corePersonalityJson);
            insert.Parameters.AddWithValue("$createdAtUtc", timestamp);
            insert.Parameters.AddWithValue("$updatedAtUtc", timestamp);
            insert.ExecuteNonQuery();
        }
        else
        {
            using var update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = """
                UPDATE companions
                SET display_name = $displayName,
                    core_personality_json = $corePersonalityJson,
                    updated_at_utc = $updatedAtUtc,
                    is_archived = 0
                WHERE companion_id = $companionId;
                """;
            update.Parameters.AddWithValue("$displayName", avatar.Name);
            update.Parameters.AddWithValue("$corePersonalityJson", corePersonalityJson);
            update.Parameters.AddWithValue("$updatedAtUtc", timestamp);
            update.Parameters.AddWithValue("$companionId", companionId);
            update.ExecuteNonQuery();
        }

        transaction.Commit();
        return GetByAvatarId(avatar.Id)
            ?? throw new InvalidOperationException("The companion identity could not be loaded after saving.");
    }

    public CompanionIdentity? GetByAvatarId(string avatarId)
    {
        EnsureInitialized();

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT companion_id, avatar_id, display_name, core_personality_json,
                   evolving_personality_json, created_at_utc, updated_at_utc, is_archived
            FROM companions
            WHERE avatar_id = $avatarId AND is_archived = 0;
            """;
        command.Parameters.AddWithValue("$avatarId", avatarId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadCompanion(reader) : null;
    }

    /// <summary>
    /// Archives a removed avatar without deleting its companion identity or interaction history.
    /// A future avatar can be attached to this identity in a later milestone.
    /// </summary>
    public void ArchiveByAvatarId(string avatarId, DateTimeOffset? now = null)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(avatarId))
        {
            throw new ArgumentException("An avatar ID is required.", nameof(avatarId));
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE companions
            SET is_archived = 1, updated_at_utc = $updatedAtUtc
            WHERE avatar_id = $avatarId;
            """;
        command.Parameters.AddWithValue("$updatedAtUtc", FormatUtc(now ?? DateTimeOffset.UtcNow));
        command.Parameters.AddWithValue("$avatarId", avatarId);
        command.ExecuteNonQuery();
    }

    public void RecordInteraction(
        string companionId,
        string eventType,
        string payloadJson = "{}",
        DateTimeOffset? occurredAtUtc = null)
    {
        EnsureInitialized();

        if (string.IsNullOrWhiteSpace(companionId))
        {
            throw new ArgumentException("A companion ID is required.", nameof(companionId));
        }

        var normalizedEventType = eventType?.Trim() ?? string.Empty;
        if (normalizedEventType.Length is < 1 or > 64)
        {
            throw new ArgumentException("Event type must contain 1 to 64 characters.", nameof(eventType));
        }

        var normalizedPayload = string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson;
        if (normalizedPayload.Length > 8192)
        {
            throw new ArgumentException("Interaction payload cannot exceed 8192 characters.", nameof(payloadJson));
        }

        using (JsonDocument.Parse(normalizedPayload))
        {
            // Validate JSON before it reaches the database.
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO interaction_events (
                event_id, companion_id, event_type, payload_json, occurred_at_utc
            )
            VALUES ($eventId, $companionId, $eventType, $payloadJson, $occurredAtUtc);
            """;
        command.Parameters.AddWithValue("$eventId", Guid.NewGuid().ToString("N"));
        command.Parameters.AddWithValue("$companionId", companionId);
        command.Parameters.AddWithValue("$eventType", normalizedEventType);
        command.Parameters.AddWithValue("$payloadJson", normalizedPayload);
        command.Parameters.AddWithValue(
            "$occurredAtUtc",
            FormatUtc(occurredAtUtc ?? DateTimeOffset.UtcNow));
        command.ExecuteNonQuery();
    }

    public IReadOnlyList<CompanionInteractionEvent> GetRecentInteractions(
        string companionId,
        int limit = 20)
    {
        EnsureInitialized();

        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 100.");
        }

        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT event_id, companion_id, event_type, payload_json, occurred_at_utc
            FROM interaction_events
            WHERE companion_id = $companionId
            ORDER BY occurred_at_utc DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$companionId", companionId);
        command.Parameters.AddWithValue("$limit", limit);

        using var reader = command.ExecuteReader();
        var events = new List<CompanionInteractionEvent>();
        while (reader.Read())
        {
            events.Add(new CompanionInteractionEvent
            {
                Id = reader.GetString(0),
                CompanionId = reader.GetString(1),
                EventType = reader.GetString(2),
                PayloadJson = reader.GetString(3),
                OccurredAtUtc = ParseUtc(reader.GetString(4))
            });
        }

        return events;
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
            throw new InvalidOperationException("Initialize the companion identity store before using it.");
        }
    }

    private static CompanionIdentity ReadCompanion(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(0),
        AvatarId = reader.GetString(1),
        DisplayName = reader.GetString(2),
        CorePersonalityJson = reader.GetString(3),
        EvolvingPersonalityJson = reader.GetString(4),
        CreatedAtUtc = ParseUtc(reader.GetString(5)),
        UpdatedAtUtc = ParseUtc(reader.GetString(6)),
        IsArchived = reader.GetInt64(7) != 0
    };

    private static string FormatUtc(DateTimeOffset value) =>
        value.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseUtc(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.None);
}
