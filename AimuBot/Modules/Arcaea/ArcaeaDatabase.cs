
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

    //floor long arc skytao
    [SqliteColumn("notes_f")] public int NotesF = 0;
    [SqliteColumn("notes_l")] public int NotesL = 0;
    [SqliteColumn("notes_a")] public int NotesA = 0;
    [SqliteColumn("notes_s")] public int NotesS = 0;
}

[SqliteTable("score")]
public class ScoreDesc
{
    public string arc_id = "";
    public string song_id = "";
    public int type = 0;
    public int diff = 0;
    public int score = 0;
    public double rating = 0;
    public int clear_type = 0;
    public int pure = 0;
    public int good = 0;
    public int far = 0;
    public int lost = 0;

    [SqliteColumn(Constraint = "unique on conflict replace default 0")]
    public long time = 0;

    public int early = 0;
    public int late = 0;
    public int early_p = 0;
    public int late_p = 0;
}

[SqliteTable("bind_info")]
class BindInfoDesc
{
    [SqliteColumn(Constraint = "primary key on conflict replace")]
    public long qq_id = 0;

    [SqliteColumn(SqliteType = "integer")]
    public string arc_id = "";

    public string name = "";
    public int bind_type = 0;
    public int b30_type = 0;
    public int recent_type = 0;
}

[SqliteTable("ptt_history")]
class PttHistoryDesc
{
    public string arc_id = "";
    public int type = 0;
    public double ptt = 0;
    public double b30 = 0;
    public double r10 = 0;
    public double ptt_local = 0;

    [SqliteColumn(Constraint = "unique on conflict replace default 0")]
    public long time = 0;
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
        using SqliteConnection? connection = new SqliteConnection(connectionString);
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

        int r = command.ExecuteNonQuery();
    }

}
