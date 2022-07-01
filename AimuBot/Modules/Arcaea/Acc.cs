using System.Text;

using AimuBot.Core.Config;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;
using AimuBot.Modules.Arcaea.AuaJson;

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea : ModuleBase
{
    [Command("acc",
        Name = "acc",
        Description = "acc",
        Tip = "/acc",
        Example = "/acc",
        Category = "Arcaea",
        Matching = Matching.StartsWith,
        Level = RbacLevel.Normal,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnAcc(BotMessage msg)
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
        LogMessage($"[Acc Info] {msg.SenderId}: {songName}, {difficulty}");

        var bindInfo = GetArcId(msg.SenderId);

        if (bindInfo == null)
            return "未绑定或id错误\n请使用/ac bind [arcaea数字id] 进行绑定";

        var arcIdOrName = bindInfo.BindType == 1 ? bindInfo.Name : bindInfo.ArcId;
        var userNameOrId = bindInfo.ArcId;

        var response = content == ""
            ? await GetUserRecent(arcIdOrName)
            : await GetUserBest(userNameOrId, songName, difficulty.ToString());

        if (response is null)
            return "查询出错。";

        if (response.Status < 0)
            return $"查询出错 {response.Status}: {response.Message}";

        bindInfo.ArcId = response.Content.AccountInfo.Code;
        bindInfo.Name = response.Content.AccountInfo.Name;
        _db.SaveObject(bindInfo);

        var contentRecord = content == "" ? response.Content.RecentScore[0] : response.Content.Record;

        UpdatePlayerScoreRecord(response.Content.AccountInfo, contentRecord);
        UpdatePttHistory(bindInfo.ArcId, response.Content.AccountInfo.RealRating,
            contentRecord.TimePlayed);

        var sb = new StringBuilder();

        sb.Append(
            $"{contentRecord.SongId} {contentRecord.Difficulty} {contentRecord.Score} {contentRecord.Rating:F2}\n");

        var ptt = response.Content.AccountInfo.Rating;

        sb.Append($"Your ptt: {response.Content.AccountInfo.PttText}\n");

        if (contentRecord.Score < 9000000)
        {
            sb.Append("No Aua rank.");
            return sb.ToString();
        }
        
        var pr = await GetPlayData(contentRecord.SongId, contentRecord.Difficulty, ptt - 10, ptt + 10);

        var allPlayers = Enumerable.Sum<ScoreDistItem>(pr.Content, x => x.Count);

        var above = Enumerable.Where<ScoreDistItem>(pr.Content, x => x.Fscore * 10000 > contentRecord.Score).Sum(x => x.Count);
        if (above == 0)
            above = 1;

        sb.Append(
            $"Aua play rank in {(float)(ptt - 10) / 100:F2}~{(float)(ptt + 10) / 100:F2}: #{above}/{allPlayers} ");

        sb.Append($"(top {(float)above * 100 / allPlayers:F2}%)\n");

        var pr2 = await GetPlayData(contentRecord.SongId, contentRecord.Difficulty, ptt - 50, ptt + 50);

        var allPlayers2 = Enumerable.Sum<ScoreDistItem>(pr2.Content, x => x.Count);

        var above2 = Enumerable.Where<ScoreDistItem>(pr2.Content, x => x.Fscore * 10000 > contentRecord.Score).Sum(x => x.Count);
        if (above2 == 0)
            above2 = 1;

        sb.Append(
            $"Aua play rank in {(float)(ptt - 50) / 100:F2}~{(float)(ptt + 50) / 100:F2}: #{above2}/{allPlayers2} ");

        sb.Append($"(top {(float)above2 * 100 / allPlayers2:F2}%)");

        return sb.ToString();
    }
}
