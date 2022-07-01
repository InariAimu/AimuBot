using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Modules.Arcaea.SlstJson;

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea : ModuleBase
{
    [Command("ac alias",
        Name = "查询别名",
        Description = "查询别名",
        Tip = "/ac alias <song_name>",
        Example = "/acs alias 妙脆角",
        Category = "Arcaea",
        State = State.Developing,
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public MessageChain OnGetAlias(BotMessage msg) => "";

    [Command("ac adda",
        Name = "添加别名",
        Description = "添加别名",
        Tip = "/ac adda <song_name>/<alias>[</|\\|_|,|\\n|，>alias]...",
        Example = "/ac adda tempestissimo/风暴，妙脆角，猫魔王，我没有买外卖",
        Category = "Arcaea",
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public MessageChain OnAddAlias(BotMessage msg)
    {
        var param = msg.Content.Split('/', '\\', '_', ',', '\n', '，');
        if (param.Length < 2)
            return "";

        var song_raw = GetSongByKeyword(param[0]);
        if (song_raw is null)
            return "";

        var count = 0;
        for (var i = 1; i < param.Length; i++) count += _arcaeaNameAlias.SaveNameAlias(song_raw.Id, param[i]);

        return $"{song_raw.Id}:\nAdded {count} records.";
    }

    [Command("ac song",
        Name = "查询歌曲",
        Description = "查询歌曲",
        Tip = "/ac song <song_name>",
        Example = "/ac song tempestissimo\n/ac song 我没有买外卖",
        Category = "Arcaea",
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public MessageChain OnGetSong(BotMessage msg)
    {
        var song_raw = GetSongByKeyword(msg.Content);
        if (song_raw is null)
            return "";

        var (title_en, title_jp) = song_raw.GetTitle();
        var tt = title_jp is null ? title_en : title_jp;
        var s = $"[{tt}]\n" +
                $"Artist: {song_raw.Artist}\n" +
                $"Pack: {song_raw.Set}\n" +
                $"BPM: {song_raw.Bpm}\n";

        var ratings = Enumerable.Select<ArcaeaSongDifficultyRaw, float>(song_raw.Difficulties, x => GetRating(song_raw.Id, song_raw.Difficulties.IndexOf(x)));
        var notes = Enumerable.Select<ArcaeaSongDifficultyRaw, int>(song_raw.Difficulties, x => GetNotes(song_raw.Id, song_raw.Difficulties.IndexOf(x)));

        if (ratings.Any())
        {
            s += "Ratings: ";
            s += string.Join(" | ", ratings.Where(x => x > 0));
            s += "\n";
        }

        var mb = new MessageBuilder();
        mb.Add(ImageChain.Create($"{song_raw.GetCover()[..^4]}_256.jpg")).Add(TextChain.Create(s));

        return mb.Build();
    }
}