using AimuBot.Core.Config;
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
        Level = RbacLevel.Normal,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnInfo(BotMessage msg)
    {
        var content = msg.Content;

        var difficultyText = content.SubstringAfterLast(" ");
        var difficulty = difficultyText.ToLower() switch
        {
            "pst" => 0,
            "prs" => 1,
            "ftr" => 2,
            "byd" => 3,
            _     => -1
        };

        var songName = "";
        if (difficulty == -1)
        {
            difficulty = 2;
            songName = content;
        }
        else
        {
            songName = content.SubstringBeforeLast(" ");
        }

        songName = TryGetSongIdByKeyword(songName);

        var (succ, bindInfo) = _db.GetObject<BindInfoDesc>(
            "qq_id = $qq_id",
            new Dictionary<string, object> { { "$qq_id", msg.SenderId } });

        if (!succ)
            return "未绑定或id错误\n请使用/ac bind [arcaea数字id] 进行绑定";

        LogMessage($"[ArcInfo] {msg.SenderId}: {songName}, {difficulty}");

        var userNameOrId = bindInfo.ArcId;

        var response = await GetUserBest(userNameOrId, songName, difficulty.ToString());
        if (response.Status < 0) return $"{response.Status}: {response.Message}";

        if (bindInfo != null)
        {
            bindInfo.ArcId = response.Content.AccountInfo.Code;
            bindInfo.Name = response.Content.AccountInfo.Name;
            _db.SaveObject(bindInfo);
        }

        var accountInfo = response.Content.AccountInfo;
        var recentScore = response.Content.Record;

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

        PttHistoryDesc pttHistoryDesc = new()
        {
            ArcId = accountInfo.Code,
            Ptt = accountInfo.RealRating,
            Time = recentScore.TimePlayed,
            Type = 0
        };
        _db.SaveObject(pttHistoryDesc);

        var im = GetRecentImage(response, bindInfo.ArcId, bindInfo.RecentType);
        if (im != null)
        {
            im.Save(BotUtil.CombinePath($"Arcaea/recents/{userNameOrId}.jpg"));
            return new MessageBuilder(ImageChain.Create($"Arcaea/recents/{userNameOrId}.jpg")).Build();
        }

        LogMessage("[ArcInfo] Render Image Error");

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
        var content = msg.Content;

        var difficultyText = content.SubstringAfterLast(" ");
        var difficulty = difficultyText.ToLower() switch
        {
            "pst" => 0,
            "prs" => 1,
            "ftr" => 2,
            "byd" => 3,
            _     => -1
        };

        string songName;
        if (difficulty == -1)
        {
            difficulty = 2;
            songName = content;
        }
        else
        {
            songName = content.SubstringBeforeLast(" ").SubstringAfter(" ");
        }

        songName = TryGetSongIdByKeyword(songName);

        var userNameOrId = content.SubstringBefore(" ");

        LogMessage($"[ArcUsrInfo] {userNameOrId}: {songName}, {difficulty}");

        var response = await GetUserBest(userNameOrId, songName, difficulty.ToString());
        if (response.Status < 0) return $"{response.Status}: {response.Message}";

        var accountInfo = response.Content.AccountInfo;

        var im = GetRecentImage(response, accountInfo.UserId, 0);
        if (im != null)
        {
            im.Save(BotUtil.CombinePath($"Arcaea/recents/{userNameOrId}.jpg"));
            return new MessageBuilder(ImageChain.Create($"Arcaea/recents/{userNameOrId}.jpg")).Build();
        }

        LogMessage("[ArcInfo] Render Image Error");

        return "";
    }
}