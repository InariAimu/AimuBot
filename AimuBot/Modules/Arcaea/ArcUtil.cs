using AimuBot.Core.ModuleMgr;
using AimuBot.Modules.Arcaea.AuaJson;
using AimuBot.Modules.Arcaea.SlstJson;

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea : ModuleBase
{
    private string TryGetSongIdByKeyword(string keyword)
    {
        var s = GetSongByKeyword(keyword);
        return s is null ? keyword : s.Id;
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
        return s ?? null;
    }

    private BindInfoDesc? GetArcId(long qqId)
    {
        var (succ, bindInfo) = _db.GetObject<BindInfoDesc>(
            $"qq_id=$qq_id",
            new Dictionary<string, object> { { "$qq_id", qqId } }
        );
        return !succ ? null : bindInfo;
    }

    private void UpdatePlayerScoreRecord(AccountInfo accountInfo, PlayRecord recentScore)
    {
        ScoreDesc scoreDesc = new()
        {
            ArcId = accountInfo.Code,
            SongId = recentScore.SongId,
            Score = recentScore.Score,
            Difficulty = recentScore.Difficulty,
            Rating = recentScore.Rating,
            ClearType = (int)recentScore.ClearType,
            Pure = recentScore.PerfectCount,
            Good = recentScore.PerfectCount - recentScore.ShinyPerfectCount,
            Far = recentScore.NearCount,
            Lost = recentScore.MissCount,
            time = recentScore.TimePlayed
        };
        _db.SaveObject(scoreDesc);
    }

    private void UpdatePttHistory(string arcId, double ptt, long time, int type = 0)
    {
        PttHistoryDesc pttHistoryDesc = new()
        {
            ArcId = arcId,
            Ptt = ptt,
            Time = time,
            Type = 0
        };
        _db.SaveObject(pttHistoryDesc);
    }

    private float GetRating(string songId, int difficulty)
    {
        var (succ, songExtra) = _db.GetObject<SongExtra>(
            "song_id = $sid and song_diff = $diff",
            new Dictionary<string, object>
            {
                { "sid", songId },
                { "diff", difficulty }
            });

        return succ ? songExtra.Rating / 10f : 0;
    }

    private int GetNotes(string songId, int difficulty)
    {
        var (succ, songExtra) = _db.GetObject<SongExtra>(
            "song_id = $sid and song_diff = $diff",
            new Dictionary<string, object>
            {
                { "sid", songId },
                { "diff", difficulty }
            });

        return succ ? songExtra.Notes : 0;
    }

    private string GetShortDifficultyText(int difficulty)
        => difficulty switch
        {
            0 => "PST",
            1 => "PRS",
            2 => "FTR",
            3 => "BYD",
            _ => "UKN"
        };

    private string GetDifficultyText(int difficulty)
        => difficulty switch
        {
            0 => "Past",
            1 => "Present",
            2 => "Future",
            3 => "Beyond",
            _ => "Unknown"
        };

    private string GetGradeText(int score, bool includePureMemory = false)
        => includePureMemory
            ? score switch
            {
                >= 10000000 => "P",
                >= 9900000  => "EX+",
                >= 9800000  => "EX",
                >= 9500000  => "AA",
                >= 9200000  => "A",
                >= 8900000  => "B",
                >= 8600000  => "C",
                _           => "D"
            }
            : score switch
            {
                >= 9900000 => "EX+",
                >= 9800000 => "EX",
                >= 9500000 => "AA",
                >= 9200000 => "A",
                >= 8900000 => "B",
                >= 8600000 => "C",
                _          => "D"
            };
}