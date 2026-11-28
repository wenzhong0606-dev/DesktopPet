using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BichonDesktopPet.Core.Models;
using Microsoft.Data.Sqlite;

namespace BichonDesktopPet.Core.Services;

/// <summary>Independent opt-in, local, fixed-axis adaptation based only on explicit user feedback.</summary>
public sealed class PersonalityGrowthStore
{
    public const int EvidenceDaysPerStep = 3;
    public const int MaximumMagnitude = 2;
    private readonly string _connectionString;
    private readonly TimeProvider _clock;

    public PersonalityGrowthStore(string databasePath, TimeProvider? clock = null)
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
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS companion_growth_settings (
                companion_id TEXT PRIMARY KEY REFERENCES companions(companion_id) ON DELETE CASCADE,
                enabled INTEGER NOT NULL DEFAULT 0 CHECK(enabled IN(0,1)),
                last_message_id INTEGER NOT NULL DEFAULT 0
            );
            CREATE TABLE IF NOT EXISTS companion_growth_axes (
                companion_id TEXT NOT NULL REFERENCES companions(companion_id) ON DELETE CASCADE,
                axis TEXT NOT NULL CHECK(axis IN('length','direct','natural','emoji')),
                score INTEGER NOT NULL DEFAULT 0 CHECK(score BETWEEN -2 AND 2),
                direction INTEGER NOT NULL DEFAULT 0,
                evidence_days INTEGER NOT NULL DEFAULT 0,
                first_day TEXT,
                last_day TEXT,
                PRIMARY KEY(companion_id,axis)
            );
            CREATE TABLE IF NOT EXISTS companion_growth_changes (
                change_id INTEGER PRIMARY KEY AUTOINCREMENT,
                companion_id TEXT NOT NULL REFERENCES companions(companion_id) ON DELETE CASCADE,
                axis TEXT NOT NULL, before_score INTEGER NOT NULL, after_score INTEGER NOT NULL,
                at_utc TEXT NOT NULL, source_message_id INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_growth_changes_companion
                ON companion_growth_changes(companion_id,change_id DESC);
            """;
        command.ExecuteNonQuery();
    }

    public PersonalityGrowthProfile Read(string companionId)
    {
        using var connection = Open();
        using var command = Cmd(connection, companionId, """
            SELECT enabled,
                COALESCE((SELECT score FROM companion_growth_axes WHERE companion_id=$id AND axis='length'),0),
                COALESCE((SELECT score FROM companion_growth_axes WHERE companion_id=$id AND axis='direct'),0),
                COALESCE((SELECT score FROM companion_growth_axes WHERE companion_id=$id AND axis='natural'),0),
                COALESCE((SELECT score FROM companion_growth_axes WHERE companion_id=$id AND axis='emoji'),0)
            FROM companion_growth_settings WHERE companion_id=$id;
            """);
        using var reader = command.ExecuteReader();
        return reader.Read() ? new PersonalityGrowthProfile(reader.GetInt64(0)==1, reader.GetInt32(1),
            reader.GetInt32(2), reader.GetInt32(3), reader.GetInt32(4)) : new PersonalityGrowthProfile();
    }

    public void SetEnabled(string companionId, bool enabled)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = Cmd(connection, companionId, """
            INSERT INTO companion_growth_settings(companion_id,enabled,last_message_id)
            VALUES($id,$enabled,COALESCE((SELECT MAX(m.message_id) FROM chat_messages m
                JOIN chat_sessions s ON s.session_id=m.session_id WHERE s.companion_id=$id),0))
            ON CONFLICT(companion_id) DO UPDATE SET enabled=excluded.enabled,
                last_message_id=MAX(last_message_id,excluded.last_message_id);
            """, transaction);
        command.Parameters.AddWithValue("$enabled", enabled ? 1 : 0);
        command.ExecuteNonQuery();
        ClearPending(connection, transaction, companionId);
        transaction.Commit();
    }

    /// <summary>Only newly persisted same-companion user messages qualify. No history backfill.</summary>
    public bool Observe(string companionId, long messageId)
    {
        if (messageId <= 0) return false;
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var lookup = Cmd(connection, companionId, """
            SELECT m.content,m.created_at_utc FROM chat_messages m
            JOIN chat_sessions s ON s.session_id=m.session_id
            JOIN companion_growth_settings g ON g.companion_id=s.companion_id
            WHERE s.companion_id=$id AND g.enabled=1 AND m.role='user'
                AND m.message_id=$message AND m.message_id>g.last_message_id;
            """, transaction);
        lookup.Parameters.AddWithValue("$message", messageId);
        string content;
        DateTimeOffset recorded;
        using (var reader = lookup.ExecuteReader())
        {
            if (!reader.Read()) return false;
            content = reader.GetString(0);
            recorded = DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture);
        }
        using (var cursor = Cmd(connection, companionId,
            "UPDATE companion_growth_settings SET last_message_id=$message WHERE companion_id=$id;", transaction))
        {
            cursor.Parameters.AddWithValue("$message", messageId);
            cursor.ExecuteNonQuery();
        }
        var signal = Signal(content);
        var changed = false;
        var now = _clock.GetUtcNow();
        if (signal is { } value && recorded <= now && now-recorded <= TimeSpan.FromDays(60))
        {
            var day = recorded.UtcDateTime.Date;
            int score = 0, direction = 0, count = 0;
            DateTime? first = null, last = null;
            using (var read = Cmd(connection, companionId, """
                SELECT score,direction,evidence_days,first_day,last_day FROM companion_growth_axes
                WHERE companion_id=$id AND axis=$axis;
                """, transaction))
            {
                read.Parameters.AddWithValue("$axis", value.Axis);
                using var reader = read.ExecuteReader();
                if (reader.Read())
                {
                    score=reader.GetInt32(0); direction=reader.GetInt32(1); count=reader.GetInt32(2);
                    first=reader.IsDBNull(3)?null:DateTime.ParseExact(reader.GetString(3),"yyyy-MM-dd",CultureInfo.InvariantCulture);
                    last=reader.IsDBNull(4)?null:DateTime.ParseExact(reader.GetString(4),"yyyy-MM-dd",CultureInfo.InvariantCulture);
                }
            }
            if (last is null || day >= last.Value)
            {
                // Conflicting same-day evidence cancels pending support; no extra day is credited.
                if (last == day)
                {
                    if (direction != value.Direction) { direction=value.Direction; count=0; first=day; }
                }
                else
                {
                    if (direction != value.Direction || first is null || day-first.Value > TimeSpan.FromDays(60))
                    { direction=value.Direction; count=0; first=day; }
                    count++;
                }
                var before = score;
                if (count >= EvidenceDaysPerStep)
                {
                    score = Math.Clamp(score + direction, value.Axis=="length" ? -MaximumMagnitude : 0, MaximumMagnitude);
                    count=0; first=day;
                }
                using (var write = Cmd(connection, companionId, """
                    INSERT INTO companion_growth_axes(companion_id,axis,score,direction,evidence_days,first_day,last_day)
                    VALUES($id,$axis,$score,$direction,$count,$first,$last)
                    ON CONFLICT(companion_id,axis) DO UPDATE SET score=excluded.score,direction=excluded.direction,
                        evidence_days=excluded.evidence_days,first_day=excluded.first_day,last_day=excluded.last_day;
                    """, transaction))
                {
                    write.Parameters.AddWithValue("$axis", value.Axis);
                    write.Parameters.AddWithValue("$score", score);
                    write.Parameters.AddWithValue("$direction", direction);
                    write.Parameters.AddWithValue("$count", count);
                    write.Parameters.AddWithValue("$first", first is { } f ? Day(f) : DBNull.Value);
                    write.Parameters.AddWithValue("$last", Day(day));
                    write.ExecuteNonQuery();
                }
                if (score != before)
                {
                    using var log = Cmd(connection, companionId, """
                        INSERT INTO companion_growth_changes(companion_id,axis,before_score,after_score,at_utc,source_message_id)
                        VALUES($id,$axis,$before,$after,$now,$message);
                        """, transaction);
                    log.Parameters.AddWithValue("$axis",value.Axis);
                    log.Parameters.AddWithValue("$before",before);
                    log.Parameters.AddWithValue("$after",score);
                    log.Parameters.AddWithValue("$now",now.ToUniversalTime().ToString("O",CultureInfo.InvariantCulture));
                    log.Parameters.AddWithValue("$message",messageId);
                    log.ExecuteNonQuery();
                    changed=true;
                }
            }
        }
        transaction.Commit();
        return changed;
    }

    public IReadOnlyList<PersonalityGrowthChange> RecentChanges(string companionId)
    {
        using var connection = Open();
        using var command = Cmd(connection, companionId, """
            SELECT change_id,axis,before_score,after_score,at_utc,source_message_id FROM companion_growth_changes
            WHERE companion_id=$id ORDER BY change_id DESC LIMIT 50;
            """);
        using var reader = command.ExecuteReader();
        var result = new List<PersonalityGrowthChange>();
        while(reader.Read()) result.Add(new(reader.GetInt64(0),reader.GetString(1),reader.GetInt32(2),
            reader.GetInt32(3),DateTimeOffset.Parse(reader.GetString(4),CultureInfo.InvariantCulture),reader.GetInt64(5)));
        return result;
    }

    public bool RollbackLast(string companionId)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        long id; string axis; int before;
        using(var command=Cmd(connection,companionId, """
            SELECT change_id,axis,before_score FROM companion_growth_changes WHERE companion_id=$id
            ORDER BY change_id DESC LIMIT 1;
            """,transaction))
        using(var reader=command.ExecuteReader())
        {
            if(!reader.Read()) return false;
            id=reader.GetInt64(0); axis=reader.GetString(1); before=reader.GetInt32(2);
        }
        using(var restore=Cmd(connection,companionId,
            "UPDATE companion_growth_axes SET score=$score WHERE companion_id=$id AND axis=$axis;",transaction))
        {
            restore.Parameters.AddWithValue("$score",before);
            restore.Parameters.AddWithValue("$axis",axis);
            restore.ExecuteNonQuery();
        }
        using(var remove=Cmd(connection,companionId,
            "DELETE FROM companion_growth_changes WHERE companion_id=$id AND change_id=$change;",transaction))
        {
            remove.Parameters.AddWithValue("$change",id); remove.ExecuteNonQuery();
        }
        ClearPending(connection,transaction,companionId);
        transaction.Commit();
        return true;
    }

    public void Reset(string companionId)
    {
        using var connection=Open();
        using var transaction=connection.BeginTransaction();
        using var command=Cmd(connection,companionId, """
            DELETE FROM companion_growth_axes WHERE companion_id=$id;
            DELETE FROM companion_growth_changes WHERE companion_id=$id;
            """,transaction);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    private static void ClearPending(SqliteConnection connection,SqliteTransaction transaction,string id)
    {
        using var command=Cmd(connection,id, """
            UPDATE companion_growth_axes SET direction=0,evidence_days=0,first_day=NULL
            WHERE companion_id=$id;
            """,transaction);
        command.ExecuteNonQuery();
    }

    private static (string Axis,int Direction)? Signal(string text) =>
        text.Trim().TrimEnd('。','！','!') switch
        {
            "我希望你回答简洁一点" => ("length",-1),
            "我希望你回答详细一点" => ("length",1),
            "我希望你回答直接一点" => ("direct",1),
            "我希望你说话自然一点" or "我希望你不要卖萌" => ("natural",1),
            "我希望你少用表情符号" or "我希望你不要使用表情符号" or "我不喜欢你使用表情符号" => ("emoji",1),
            _ => null
        };

    private static string Day(DateTime day)=>day.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
    private SqliteConnection Open()
    {
        var connection=new SqliteConnection(_connectionString);
        try { connection.Open(); return connection; } catch { connection.Dispose(); throw; }
    }
    private static SqliteCommand Cmd(SqliteConnection connection,string id,string sql,SqliteTransaction? transaction=null)
    {
        if(string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Companion ID required.",nameof(id));
        var command=connection.CreateCommand(); command.Transaction=transaction; command.CommandText=sql;
        command.Parameters.AddWithValue("$id",id); return command;
    }
}
