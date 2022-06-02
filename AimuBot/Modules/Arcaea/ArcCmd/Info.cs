
using AimuBot.Core.Bot;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea
{
    [Command("ac info",
        Name = "查询指定谱面成绩",
        Description = "查询指定谱面成绩",
        Tip = "/ac info <song_name> [difficulty=ftr]",
        Example = "/ac info ac\n/ac info tempestissimo byd\n/ac info 猫魔王",
        Category = "Arcaea",
        CooldownType = CooldownType.User,
        CooldownSecond = 10,
        Matching = Matching.StartsWith,
        Level = RBACLevel.Normal,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnInfo(BotMessage msg)
    {
        string? content = msg.Content;

        string difficulty_str = content.SubstringAfterLast(" ");
        int difficulty = difficulty_str.ToLower() switch
        {
            "pst" => 0,
            "prs" => 1,
            "ftr" => 2,
            "byd" => 3,
            _ => -1,
        };

        string song_name = "";
        if (difficulty == -1)
        {
            difficulty = 2;
            song_name = content;
        }
        else
        {
            song_name = content.SubstringBeforeLast(" ");
        }

        song_name = TryGetSongIdByKeyword(song_name);

        var (succ, bind_info) = _db.GetObject<BindInfoDesc>(
            "qq_id = $qq_id",
            new() { { "$qq_id", msg.SenderId } });

        if (!succ)
            return "未绑定或id错误\n请使用/ac bind [arcaea数字id] 进行绑定";

        LogMessage($"[ArcInfo] {msg.SenderId}: {song_name}, {difficulty}");

        string? user_name_or_id = bind_info.arc_id;

        var response = await GetUserBest(user_name_or_id, song_name, difficulty.ToString());
        if (response.Status < 0)
        {
            return $"{response.Status}: {response.Message}";
        }

        if (bind_info != null)
        {
            bind_info.arc_id = response.Content.AccountInfo.Code;
            bind_info.name = response.Content.AccountInfo.Name;
            _db.SaveObject(bind_info);
        }

        var account_info = response.Content.AccountInfo;
        var recent_score = response.Content.Record;

        ScoreDesc score_desc = new()
        {
            arc_id = account_info.Code,
            song_id = recent_score.SongId,
            score = recent_score.Score,
            diff = recent_score.Difficulty,
            rating = recent_score.Rating,
            clear_type = (int)recent_score.ClearType,
            pure = recent_score.PerfectCount,
            good = recent_score.PerfectCount - recent_score.ShinyPerfectCount,
            far = recent_score.NearCount,
            lost = recent_score.MissCount,
            time = recent_score.TimePlayed,
        };
        _db.SaveObject(score_desc);

        PttHistoryDesc pttHistoryDesc = new()
        {
            arc_id = account_info.Code,
            ptt = account_info.RealRating,
            time = recent_score.TimePlayed,
            type = 0,
        };
        _db.SaveObject(pttHistoryDesc);

        var im = GetRecentImage(response, bind_info.arc_id, bind_info.recent_type);
        if (im != null)
        {
            im.Save(BotUtil.CombinePath($"Arcaea/recents/{user_name_or_id}.jpg"));
            return new MessageBuilder(ImageChain.Create($"Arcaea/recents/{user_name_or_id}.jpg")).Build();
        }
        else
        {
            LogMessage("[ArcInfo] Render Image Error");
        }

        return "";
    }

    [Command("ac usrinfo",
        Name = "指定玩家指定谱面成绩",
        Description = "获取指定玩家指定谱面的游玩成绩",
        Tip = "/ac usrinfo <arc_id> <song_id> [difficulty=ftr]",
        Example = "/ac usrinfo NitroX72 lfdy\n/ac usrinfo 062596721 lfdy",
        Category = "Arcaea",
        CooldownType = CooldownType.User,
        CooldownSecond = 10,
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnRecentUsrInfo(BotMessage msg)
    {
        string? content = msg.Content;

        string difficulty_str = content.SubstringAfterLast(" ");
        int difficulty = difficulty_str.ToLower() switch
        {
            "pst" => 0,
            "prs" => 1,
            "ftr" => 2,
            "byd" => 3,
            _ => -1,
        };

        string song_name;
        if (difficulty == -1)
        {
            difficulty = 2;
            song_name = content;
        }
        else
        {
            song_name = content.SubstringBeforeLast(" ").SubstringAfter(" ");
        }

        song_name = TryGetSongIdByKeyword(song_name);

        string? user_name_or_id = content.SubstringBefore(" ");

        LogMessage($"[ArcUsrInfo] {user_name_or_id}: {song_name}, {difficulty}");

        var response = await GetUserBest(user_name_or_id, song_name, difficulty.ToString());
        if (response.Status < 0)
        {
            return $"{response.Status}: {response.Message}";
        }

        var account_info = response.Content.AccountInfo;

        var im = GetRecentImage(response, account_info.UserId, 0);
        if (im != null)
        {
            im.Save(BotUtil.CombinePath($"Arcaea/recents/{user_name_or_id}.jpg"));
            return new MessageBuilder(ImageChain.Create($"Arcaea/recents/{user_name_or_id}.jpg")).Build();
        }
        else
        {
            LogMessage("[ArcInfo] Render Image Error");
        }

        return "";
    }
}
