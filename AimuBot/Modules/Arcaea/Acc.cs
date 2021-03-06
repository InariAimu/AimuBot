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
        Name = "Aua 排名查询",
        Description = "查询您在 ptt 接近的玩家中的排名情况。可用来分析是否虚高或虚低。",
        BlocksBefore = new[]
        {
            "::: warning 注意\n**此命令会占用大量查分资源，请勿滥用**，可能需要较长时间响应，不要重复查询。\n" +
            "此结果仅表明您在**使用 Aua 查过分的玩家**中的排名，不反映真实的排行榜数据，因此可能偏低。仅供参考。\n:::"
        },
        Template = "/acc [<song_name> [difficulty=ftr]]",
        NekoBoxExample =
            "{ position: 'right', msg: '/acc' }," +
            "{ position: 'left', chain:[ {'reply': '/acc'}, { msg: 'specta 2 9316448 8.89\\nYour ptt: 10.50\\nAua play rank in 10.40~10.60: #197/208 (top 94.71%)\\nAua play rank in 10.00~11.00: #1010/1066 (top 94.75%)' }] }," +
            "{ position: 'right', msg: '/acc vividtheory' }," +
            "{ position: 'left', chain:[ {'reply': '/acc vividtheory'}, { msg: 'vividtheory 2 10000850 10.80\\nYour ptt: 10.50\\nAua play rank in 10.40~10.60: #1/460 (top 0.22%)\\nAua play rank in 10.00~11.00: #1/2184 (top 0.05%)' }] },",
        Category = "Arcaea",
        Matching = Matching.StartsWith,
        Level = RbacLevel.Normal,
        State = State.Test,
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

        var allPlayers = pr.Content.Sum(x => x.Count);

        var above = pr.Content.Where(x => x.Fscore * 10000 > contentRecord.Score)
            .Sum(x => x.Count);
        if (above == 0)
            above = 1;

        sb.Append(
            $"Aua play rank in {(float)(ptt - 10) / 100:F2}~{(float)(ptt + 10) / 100:F2}: #{above}/{allPlayers} ");

        sb.Append($"({(float)above * 100 / allPlayers:F2}%) avg{(int)pr.Content.Average(x=>x.Fscore)}\n");

        var pr2 = await GetPlayData(contentRecord.SongId, contentRecord.Difficulty, ptt - 50, ptt + 50);

        var allPlayers2 = Enumerable.Sum<ScoreDistItem>(pr2.Content, x => x.Count);

        var above2 = Enumerable.Where<ScoreDistItem>(pr2.Content, x => x.Fscore * 10000 > contentRecord.Score)
            .Sum(x => x.Count);
        if (above2 == 0)
            above2 = 1;

        sb.Append(
            $"Aua play rank in {(float)(ptt - 50) / 100:F2}~{(float)(ptt + 50) / 100:F2}: #{above2}/{allPlayers2} ");

        sb.Append($"({(float)above2 * 100 / allPlayers2:F2}%) avg{(int)pr2.Content.Average(x=>x.Fscore)}");

        return sb.ToString();
    }
}