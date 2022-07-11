using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;
using AimuBot.Modules.Arcaea.AuaJson;

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea : ModuleBase
{
    [Command("ac yyw b30",
        Name = "音游王生成（b30）",
        Description = "生成一张音游王 b30 成绩图（即最高理论 ptt 值）",
        BlocksBefore = new[] { "::: warning 注意\n请勿滥用\n:::" },
        Template = "/ac yyw b30",
        Example = "/ac yyw b30",
        Category = "Arcaea",
        Matching = Matching.Exact,
        SendType = SendType.Reply)]
    public MessageChain OnYYWB30(BotMessage msg)
    {
        var bydSort = _db.GetObjects<SongExtra>("where [song_diff]=3 order by rating desc limit 0,30");

        var list = Enumerable.Select<SongExtra, RatingSong>(bydSort, song => new RatingSong
            {
                SongId = song.SongId, Rating = song.Rating, Notes = song.Notes, Difficulty = 3
            })
            .ToList();

        var ftrSort = _db.GetObjects<SongExtra>("where [song_diff]=2 order by rating desc limit 0,30");

        list.AddRange(Enumerable.Select<SongExtra, RatingSong>(ftrSort, song => new RatingSong
        {
            SongId = song.SongId, Rating = song.Rating, Notes = song.Notes, Difficulty = 2
        }));

        list = list.OrderByDescending(x => x.Rating).Take(30).ToList();

        var (succ, bindInfo) = _db.GetObject<BindInfoDesc>(
            "qq_id = $qq_id",
            new Dictionary<string, object> { { "$qq_id", msg.SenderId } }
        );

        if (!succ)
            return "";

        Response response = new()
        {
            Content = new Content
            {
                AccountInfo = new AccountInfo
                {
                    IsCharUncapped = true,
                    Character = 2,
                    Name = bindInfo.Name,
                    Rating = 1305
                },
                Best30List = new List<PlayRecord>(30)
            }
        };

        foreach (var song in list)
        {
            PlayRecord? record = new()
            {
                SongId = song.SongId,
                Rating = (double)song.Rating / 10 + 2,
                Difficulty = song.Difficulty,
                Health = 100,
                ClearType = 3,
                Score = 10000000 + song.Notes,
                PerfectCount = song.Notes,
                ShinyPerfectCount = song.Notes,
                TimePlayed = BotUtil.Timestamp - Core.Bot.Random.Next(2, 24) * 3214 * 1000
            };
            response.Content.Best30List.Add(record);
        }

        response.Content.Best30Avg = list.Average(song => (double)song.Rating / 10 + 2);
        response.Content.Recent10Avg = list.Take(10).Average(song => (double)song.Rating / 10 + 2);

        succ = GetB30ImageFile(response, BotUtil.CombinePath("Arcaea/yyw_b30.jpg"), 2);

        return succ ? new MessageBuilder(ImageChain.Create("Arcaea/yyw_b30.jpg")).Build() : "";
    }

    [Command("ac yyw",
        Name = "音游王生成",
        Description = "生成指定谱面的理论值成绩图",
        BlocksBefore = new[] { "::: warning 注意\n请勿滥用\n:::" },
        Template = "/ac yyw <song_name> [difficulty=ftr]",
        Example = "/ac yyw 猫光 byd\n/ac yyw 病女",
        Category = "Arcaea",
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public MessageChain OnYYW(BotMessage msg)
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
            new Dictionary<string, object> { { "$qq_id", msg.SenderId } }
        );

        if (!succ) return "未绑定或id错误\n请使用/ac bind <arcaea数字id> 进行绑定";

        var sr = _songInfoRaw.Songs.SongList.Find(x => x.Id == songName);

        if (sr != null && succ)
        {
            var notes = GetNotes(songName, difficulty);

            var response = new Response
            {
                Content = new Content
                {
                    AccountInfo = new AccountInfo
                    {
                        IsCharUncapped = true,
                        Character = 2,
                        Name = bindInfo.Name,
                        Rating = 1305
                    },
                    RecentScore = new List<PlayRecord>
                    {
                        new()
                        {
                            SongId = sr.Id,
                            Score = 10000000 + notes,
                            PerfectCount = notes,
                            ShinyPerfectCount = notes,
                            Health = 100,
                            ClearType = 3,
                            Difficulty = difficulty,
                            Rating = 0,
                            TimePlayed = BotUtil.Timestamp - 1000 * 60
                        }
                    }
                }
            };

            var im = GetRecentImage_Arcaea(response, bindInfo.ArcId, Core.Bot.Random.Next(1, 100));
            im.SaveToJpg(BotUtil.CombinePath("Arcaea/recents/yyw.jpg"));

            return new MessageBuilder(ImageChain.Create("Arcaea/recents/yyw.jpg")).Build();
        }

        LogMessage("[yyw] song not find");
        return "";
    }

    private class RatingSong
    {
        public int Rating { get; set; }
        public string SongId { get; set; }
        public int Difficulty { get; set; }
        public int Notes { get; set; }
    }
}