using AimuBot.Core.ModuleMgr;
using AimuBot.Modules.Arcaea.AuaJson;
using AimuBot.Modules.Arcaea.SlstJson;

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea : ModuleBase
{
    private string TryGetSongIdByKeyword(string keyword)
    {
        var s = GetSongByKeyword(keyword);
        if (s is null)
            return keyword;
        else
            return s.Id;
    }

    private ArcaeaSongRaw? GetSongByKeyword(string keyWord)
    {
        keyWord = keyWord.ToLower();

        // find sid by alias
        keyWord = _arcaeaNameAlias.TryGetNameByAlias(keyWord);

        // match sid or title
        var s = _songInfoRaw.Songs.SongList.Find(x => x.Id == keyWord);
        if (s is not null)
            return s;

        s = _songInfoRaw.Songs.SongList.Find(x => x.IsTitleMatch(keyWord));
        if (s is not null)
            return s;

        return null;
    }

    private BindInfoDesc? GetArcId(long qqId)
    {
        var (succ, bind_info_t) = _db.GetObject<BindInfoDesc>(
                    $"qq_id=$qq_id",
                    new() { { "$qq_id", qqId } }
                    );
        if (!succ)
            return null;

        return bind_info_t;
    }

    private void UpdatePlayerScoreRecord(AccountInfo accountInfo, PlayRecord recentScore)
    {
        ScoreDesc scoreDesc = new()
        {
            arc_id = accountInfo.Code,
            song_id = recentScore.SongId,
            score = recentScore.Score,
            diff = recentScore.Difficulty,
            rating = recentScore.Rating,
            clear_type = (int)recentScore.ClearType,
            pure = recentScore.PerfectCount,
            good = recentScore.PerfectCount - recentScore.ShinyPerfectCount,
            far = recentScore.NearCount,
            lost = recentScore.MissCount,
            time = recentScore.TimePlayed,
        };
        _db.SaveObject(scoreDesc);
    }

    private void UpdatePttHistory(string arcId, double ptt, long time, int type = 0)
    {
        PttHistoryDesc pttHistoryDesc = new()
        {
            arc_id = arcId,
            ptt = ptt,
            time = time,
            type = 0,
        };
        _db.SaveObject(pttHistoryDesc);
    }

    private float GetRating(string songId, int difficulty)
    {
        var (succ, songExtra) = _db.GetObject<SongExtra>(
            "song_id = $sid and song_diff = $diff",
            new()
            {
                { "sid", songId },
                { "diff", difficulty }
            });

        if (succ)
        {
            return songExtra.Rating / 10f;
        }
        return 0;
    }

    private int GetNotes(string songId, int difficulty)
    {
        var (succ, songExtra) = _db.GetObject<SongExtra>(
            "song_id = $sid and song_diff = $diff",
            new()
            {
                { "sid", songId },
                { "diff", difficulty }
            });

        if (succ)
        {
            return songExtra.Notes;
        }
        return 0;
    }

    private string GetShortDiffcultyStr(int difficulty)
        => difficulty switch
        {
            0 => "PST",
            1 => "PRS",
            2 => "FTR",
            3 => "BYD",
            _ => "UKN",
        };

    private string GetDiffcultyStr(int difficulty)
        => difficulty switch
        {
            0 => "Past",
            1 => "Present",
            2 => "Future",
            3 => "Beyond",
            _ => "Unknown",
        };

    private string GetGradeStr(int score, bool includePureMemory = false)
        => includePureMemory ? score switch
        {
            >= 10000000 => "P",
            >= 9900000 => "EX+",
            >= 9800000 => "EX",
            >= 9500000 => "AA",
            >= 9200000 => "A",
            >= 8900000 => "B",
            >= 8600000 => "C",
            _ => "D"
        } : score switch
        {
            >= 9900000 => "EX+",
            >= 9800000 => "EX",
            >= 9500000 => "AA",
            >= 9200000 => "A",
            >= 8900000 => "B",
            >= 8600000 => "C",
            _ => "D"
        };
}
