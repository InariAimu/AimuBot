using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;
using AimuBot.Modules.Arcaea.AuaJson;

namespace AimuBot.Modules.Arcaea
{
    public partial class Arcaea : ModuleBase
    {
        class RatingSong
        {
            public int Rating { get; set; }
            public string SongId { get; set; }
            public int Difficulty { get; set; }
            public int Notes { get; set; }
        }

        [Command("ac yyw b30",
            Name = "音游王生成（b30）",
            Description = "生成一张音游王b30成绩图（请勿滥用）",
            Tip = "/ac yyw b30",
            Example = "/ac yyw b30",
            Category = "Arcaea",
            Matching = Matching.Full,
            SendType = SendType.Reply)]
        public MessageChain OnYYWB30(BotMessage msg)
        {
            List<RatingSong> list = new();

            var bydSort = _db.GetObjects<SongExtra>("where [song_diff]=3 order by rating desc limit 0,30");

            foreach (var song in bydSort)
            {
                RatingSong rs = new()
                {
                    SongId = song.SongId,
                    Rating = song.Rating,
                    Notes = song.Notes,
                    Difficulty = 3,
                };
                list.Add(rs);
            }

            var ftrSort = _db.GetObjects<SongExtra>("where [song_diff]=2 order by rating desc limit 0,30");

            foreach (var song in ftrSort)
            {
                RatingSong rs = new()
                {
                    SongId = song.SongId,
                    Rating = song.Rating,
                    Notes = song.Notes,
                    Difficulty = 2,
                };
                list.Add(rs);
            }

            list = list.OrderByDescending(x => x.Rating).Take(30).ToList();

            var (succ, bind_info) = _db.GetObject<BindInfoDesc>(
                "qq_id = $qq_id",
                new() { { "$qq_id", msg.SenderId } }
                );

            if (!succ)
                return "";

            Response response = new()
            {
                Content = new Content()
                {
                    AccountInfo = new AccountInfo()
                    {
                        IsCharUncapped = true,
                        Character = 2,
                        Name = bind_info.name,
                        Rating = 1288,
                    },
                }
            };

            response.Content.Best30List = new List<PlayRecord>(30);

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
                    TimePlayed = BotUtil.Timestamp - Core.AimuBot.Random.Next(2, 24) * 3214 * 1000,
                };
                response.Content.Best30List.Add(record);
            }

            response.Content.Best30Avg = list.Average(song => (double)song.Rating / 10 + 2);
            response.Content.Recent10Avg = list.Take(10).Average(song => (double)song.Rating / 10 + 2);

            succ = GetB30ImageFile(response, BotUtil.CombinePath("Arcaea/yyw_b30.jpg"), 2);

            if (succ)
                return new MessageBuilder(ImageChain.Create("Arcaea/yyw_b30.jpg")).Build();

            return "";
        }

        [Command("ac yyw",
            Name = "音游王生成",
            Description = "生成一张音游王理论值成绩图（请勿滥用）",
            Tip = "/ac yyw <song_name> [difficulty=ftr]",
            Example = "/ac yyw tempestissimo byd\n/ac yyw 病女",
            Category = "Arcaea",
            Matching = Matching.StartsWith,
            SendType = SendType.Reply)]
        public MessageChain OnYYW(BotMessage msg)
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
                new() { { "$qq_id", msg.SenderId } }
                );
            if (succ)
            {
                var sr = _songInfoRaw.Songs.SongList.Find(x => x.Id == song_name);

                var (succ2, sd) = _db.GetObject<ScoreDesc>(
                    "song_id = $song_id and diff = $diff",
                    new() { { "$song_id", song_name }, { "$diff", difficulty } }
                    );

                if (sr != null && succ)
                {
                    int notes = sd.pure + sd.far + sd.lost;

                    Response response = new Response()
                    {
                        Content = new Content()
                        {
                            AccountInfo = new AccountInfo()
                            {
                                IsCharUncapped = true,
                                Character = 2,
                                Name = bind_info.name,
                                Rating = 1288,
                            },
                            RecentScore = new List<PlayRecord>()
                            {
                                new PlayRecord()
                                {
                                    SongId = sr.Id,
                                    Score = 10000000 + notes,
                                    PerfectCount = notes,
                                    ShinyPerfectCount = notes,
                                    Health = 100,
                                    ClearType = 3,
                                    Difficulty = difficulty,
                                    Rating = 0,
                                    TimePlayed = BotUtil.Timestamp - 1000*60,
                                }
                            }
                        }
                    };

                    var im = GetRecentImage_Arcaea(response, bind_info.arc_id, Core.AimuBot.Random.Next(1, 100));
                    im.SaveToJpg(BotUtil.CombinePath("Arcaea/recents/yyw.jpg"), 100);

                    return new MessageBuilder(ImageChain.Create("Arcaea/recents/yyw.jpg")).Build();
                }
                else
                {
                    LogMessage("[yyw] song not find");
                    return "";
                }
            }
            else
            {
                return "未绑定或id错误\n请使用/ac bind <arcaea数字id> 进行绑定";
            }
        }
    }
}
