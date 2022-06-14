using AimuBot.Data;

using Microsoft.Data.Sqlite;

namespace AimuBot.Modules.Arcaea;

[SqliteTable("song_extra", ColumnConstraint = "unique ([song_id], [song_diff]) on conflict ignore")]
public class SongExtra
{
    [SqliteColumn("song_id")] public string SongId = "";
    [SqliteColumn("song_diff")] public int Difficulty = 0;

    [SqliteColumn("rating")] public int Rating = 0;

    [SqliteColumn("notes")] public int Notes = 0;

    //floor long arc skyTap
    [SqliteColumn("notes_f")] public int NotesF = 0;
    [SqliteColumn("notes_l")] public int NotesL = 0;
    [SqliteColumn("notes_a")] public int NotesA = 0;
    [SqliteColumn("notes_s")] public int NotesS = 0;
}

[SqliteTable("score")]
public class ScoreDesc
{
    [SqliteColumn("arc_id")] public string ArcId = "";
    [SqliteColumn("song_id")] public string SongId = "";

    [SqliteColumn("type")] public int Type = 0;
    [SqliteColumn("diff")] public int Difficulty = 0;
    [SqliteColumn("score")] public int Score = 0;
    [SqliteColumn("rating")] public double Rating = 0;

    [SqliteColumn("clear_type")] public int ClearType = 0;

    [SqliteColumn("pure")] public int Pure = 0;
    [SqliteColumn("good")] public int Good = 0;
    [SqliteColumn("far")] public int Far = 0;
    [SqliteColumn("lost")] public int Lost = 0;

    [SqliteColumn("time", Constraint = "unique on conflict replace default 0")]
    public long time = 0;

    [SqliteColumn("early")] public int Early = 0;
    [SqliteColumn("late")] public int Late = 0;
    [SqliteColumn("early_p")] public int EarlyPure = 0;
    [SqliteColumn("late_p")] public int LatePure = 0;
}

[SqliteTable("bind_info")]
internal class BindInfoDesc
{
    [SqliteColumn("qq_id", Constraint = "primary key on conflict replace")]
    public long QqId = 0;

    [SqliteColumn("arc_id", SqliteType = "integer")]
    public string ArcId = "";

    [SqliteColumn("name")] public string Name = "";

    [SqliteColumn("bind_type")] public int BindType = 0;
    [SqliteColumn("b30_type")] public int B30Type = 0;
    [SqliteColumn("recent_type")] public int RecentType = 0;
}

[SqliteTable("ptt_history")]
internal class PttHistoryDesc
{
    [SqliteColumn("arc_id")] public string ArcId = "";
    [SqliteColumn("type")] public int Type = 0;
    [SqliteColumn("ptt")] public double Ptt = 0;
    [SqliteColumn("b30")] public double B30 = 0;
    [SqliteColumn("r10")] public double R10 = 0;
    [SqliteColumn("b30_max")] public double MaxB30 = 0;
    [SqliteColumn("ptt_local")] public double PttLocal = 0;

    [SqliteColumn("time", Constraint = "unique on conflict replace default 0")]
    public long Time = 0;
}

public class ArcaeaDatabase : SqliteDatabase
{
    public ArcaeaDatabase() : base("Arcaea/Arcaea.db")
    {
    }

    public void CreateTables()
        => CreateTables(typeof(BindInfoDesc), typeof(ScoreDesc), typeof(PttHistoryDesc), typeof(SongExtra));

    public void SaveInt(long qqId, string key, int value)
    {
        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText =
            $@"
                    INSERT INTO bind_info
                    (qq_id, [{key}])
                    VALUES
                    ($id, $value)
                    ON CONFLICT([qq_id]) DO 
                    UPDATE SET [{key}] = $value;
                ";
        command.Parameters.AddWithValue("$id", qqId);
        command.Parameters.AddWithValue("$value", value);

        var r = command.ExecuteNonQuery();
    }
}