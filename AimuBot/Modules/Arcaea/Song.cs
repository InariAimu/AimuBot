using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Modules.Arcaea.SlstJson;

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea : ModuleBase
{
    [Command("ac alias",
        Name = "查询别名",
        Description = "查询某歌曲的别名。",
        Template = "/ac alias <song_name>",
        NekoBoxExample =
            "{ position: 'right', msg: '/ac alias 妙脆角' }," +
            "{ position: 'left', chain: [ { reply: '/ac alias 妙脆角' }, { img: '/images/Arcaea/t.webp' }, { msg: '[Tempestissimo]\\n[1] 奥运会\\n[2] 妙脆角\\n[3] 对立打电话\\n[4] 对立点外卖\\n[5] 我没有买外卖\\n[6] 我要杀光光\\n[7] 暴风雨\\n[8] 猫对立\\n[9] 猫魔王\\n[10] 电话拨号\\n[11] 电话来啰\\n[12] 风暴' } ] },",
        Category = "Arcaea",
        State = State.Test,
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public MessageChain OnGetAlias(BotMessage msg)
    {
        var songRaw = GetSongByKeyword(msg.Content);
        if (songRaw is null)
            return "";

        var alias = _arcaeaNameAlias.GetAlias(songRaw.Id);

        var (titleEn, titleJp) = songRaw.GetTitle();
        var tt = titleJp is null ? titleEn : titleJp;
        var s = $"[{tt}]\n";

        for (var i = 0; i < alias.Length; i++) s += $"[{i + 1}] {alias[i]}\n";

        var mb = new MessageBuilder();
        mb.Add(ImageChain.Create($"{songRaw.GetCover()[..^4]}_256.jpg")).Add(TextChain.Create(s));

        return mb.Build();
    }

    [Command("ac adda",
        Name = "添加别名",
        Description = "为指定歌曲添加别名。分隔符可以为 `</|\\|_|,|\\n|，>` 中的任意一个（建议使用 `，`）",
        Template = "/ac adda <song_name>/<alias>[</|\\|_|,|\\n|，>alias]...",
        NekoBoxExample =
            "{ position: 'right', msg: '/ac adda tempestissimo/风暴，妙脆角，猫魔王，我没有买外卖' }," +
            "{ position: 'left', chain: [ { reply: '/ac adda tempestissimo/风暴，妙脆角，猫魔王，我没有买外卖' }, { msg: '[Tempestissimo]\\nAdded 4 records.' } ] },",
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
        Name = "查询歌曲信息",
        Description = "查询歌曲信息，包括曲绘、定数等（可使用别名查询）",
        Template = "/ac song <song_name>",
        NekoBoxExample =
            "{ position: 'right', msg: '/ac alias 猫光' }," +
            "{ position: 'left', chain: [ { reply: '/ac alias 猫光' }, { img: '/images/Arcaea/t2.webp' }, { msg: '[Testify]\\nArtist: void (Mournfinale) feat. 星熊南巫\\nPack: finale\\nBPM: 178\\nRatings: 7 | 9.4 | 10.8 | 12' } ] },",
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

        var ratings = Enumerable.Select<ArcaeaSongDifficultyRaw, float>(song_raw.Difficulties,
            x => GetRating(song_raw.Id, song_raw.Difficulties.IndexOf(x)));
        var notes = Enumerable.Select<ArcaeaSongDifficultyRaw, int>(song_raw.Difficulties,
            x => GetNotes(song_raw.Id, song_raw.Difficulties.IndexOf(x)));

        var ratingStrs = song_raw.Difficulties.Select(x => song_raw.GetGameRatingStr(song_raw.Difficulties.IndexOf(x)));

        if (ratings.Any())
        {
            s += "Ratings: ";
            if (ratings.All(x => x > 0))
                s += string.Join(" | ", ratings.Where(x => x > 0));
            else
                s += string.Join(" | ", ratingStrs);
            s += "\n";
        }

        var mb = new MessageBuilder();
        mb.Add(ImageChain.Create($"{song_raw.GetCover()[..^4]}_256.jpg")).Add(TextChain.Create(s));

        return mb.Build();
    }
}