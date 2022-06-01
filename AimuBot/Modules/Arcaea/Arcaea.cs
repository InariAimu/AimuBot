using System.Text.RegularExpressions;

using AimuBot.Core.Bot;
using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;
using AimuBot.Modules.Arcaea.AffTools.Aff2Preview;

namespace AimuBot.Modules.Arcaea;

[Module("Arcaea",
    Command = "arcaea",
    Description = "提供Arcaea查分，b30，谱面预览等功能",
    EULA = "您应知悉，使用本模块进行Arcaea查分将违反*Arcaea使用条款 3.2-4 和 3.2-6*，以及*Arcaea二次创作管理条例*。\n因使用本功能而造成的损失（包括但不限于账号被lowiro封禁、shadowban等），Aimubot开发组不予承担任何责任。",
    Privacy = "使用查分将默认您允许Aimubot收集/记录关于您的使用记录，包括且不限于Arcaea用户名、游玩记录等。\n您的数据将用于历史ptt显示、推分建议等功能。\n我们将使用行业安全标准来保存数据并不会提供给任何第三方。")]
public partial class Arcaea : ModuleBase
{
    private ArcaeaDatabase _db = null!;
    private readonly ArcaeaNameAlias _arcaeaNameAlias = new();

    private ArcaeaSongListRawWrapper _songInfoRaw = new();

    public ArcaeaDatabase GetDB() => _db;

    public override bool OnInit()
    {
        _db = new();
        _db.CreateTables();
        return true;
    }

    public override bool OnReload()
    { 
        _songInfoRaw.LoadFromArcaeaAPPSongList();
        return true;
    }

    [Command("acs",
        Name = "保存分数",
        Description = "在不查询或无法查询成绩时保存分数",
        Tip = "/acs <song_name> [difficulty=ftr] <score>",
        Example = "/acs tempestissimo byd 10001540",
        Category = "Arcaea",
        State = State.Test,
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public MessageChain OnStoreLocalScore(BotMessage msg)
    {
        string? content = msg.Content;
        string[] param_arr = content.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        ScoreDesc sd = new();
        sd.type = 1;
        sd.diff = 2;

        var m = new Regex(@"\s\d{8}").Match(content);
        if (!m.Success)
            return "";

        string? scoreStr = m.Value;
        content = content.Replace(scoreStr, "");

        var md = new Regex(@"\s(pst|prs|ftr|byd)").Match(content);
        if (m.Success)
        {
            string? diffStr = m.Value;
            content = content.Replace(diffStr, "");
            sd.diff = diffStr.ToLower() switch
            {
                "pst" => 0,
                "prs" => 1,
                "ftr" => 2,
                "byd" => 3,
                _ => 2,
            };
        }

        string? songId = TryGetSongIdByKeyword(content.Trim());

        var song = _songInfoRaw.Songs.SongList.Find(x => x.Id == songId);
        if (song is null)
            return "";

        float rating_f = GetRating(songId, sd.diff);

        sd.time = BotUtil.Timestamp;
        //db.SaveScore(sd);
        return "保存成功";
    }

    [Command("ac bind",
        Name = "绑定 Arcaea",
        Description = "绑定 ArcaeaId 或 Name（推荐使用数字 id）",
        Tip = "/ac bind <arc_id>",
        Example = "/ac bind nagiha0798\n/ac bind 000000001",
        Category = "Arcaea",
        Matching = Matching.StartsWith,
        Level = RBACLevel.Normal,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnBind(BotMessage msg)
    {
        string? content = msg.Content;
        LogMessage($"Bind: {content} -> {msg.SenderId}");

        Regex arc_id_regex = new(@"^\d{9}$");
        Regex arc_name_regex = new(@"^[A-Za-z\d_]+$");

        if (arc_id_regex.IsMatch(content) || arc_name_regex.IsMatch(content))
        {
            var response = await GetUserRecent(content);
            if (response.Status < 0)
                return $"错误：{response.Status} {response.Message}";

            var (succ, bind_info) = _db.GetObject<BindInfoDesc>(
                "arc_id = $arc_id",
                new() { { "$arc_id", response.Content.AccountInfo.Code } }
                );
            if (succ)
            {
                bind_info.bind_type = 0;
                bind_info.qq_id = msg.SenderId;
                bind_info.arc_id = content;
                bind_info.name = response.Content.AccountInfo.Name;
            }
            else
            {
                bind_info = new()
                {
                    bind_type = 0,
                    qq_id = msg.SenderId,
                    arc_id = response.Content.AccountInfo.Code,
                    name = response.Content.AccountInfo.Name,
                    b30_type = 2,
                    recent_type = 0,
                };
            }
            _db.SaveObject(bind_info);
            return $"已绑定 {response.Content.AccountInfo.Name}/{response.Content.AccountInfo.Code} ({response.Content.AccountInfo.PttText})";
        }
        else
        {
            return "格式不正确：请使用Arcaea数字id或者角色名进行绑定";
        }
    }

    [Command("ac chart",
        Name = "获取2d谱面预览",
        Description = "获取2d谱面预览",
        Tip = "/ac chart <song_name> [difficulty=ftr]",
        Example = "/ac chart 猫魔王 byd\n/ac chart dropdead pst\nac chart ifi",
        Category = "Arcaea",
        Matching = Matching.StartsWith,
        Level = RBACLevel.Normal,
        SendType = SendType.Send)]
    public MessageChain OnChart(BotMessage msg)
    {
        string? content = msg.Content;
        int diff = 2;
        string name = "";
        if (content.Length < 3)
        {
            name = content;
        }
        else
        {
            string _difficulty = content[^3..];
            (diff, name) = _difficulty.ToLower() switch
            {
                "pst" => (0, content[..^4]),
                "prs" => (1, content[..^4]),
                "ftr" => (2, content[..^4]),
                "byd" => (3, content[..^4]),
                _ => (2, content)
            };
        }

        name = name.Trim();
        name = TryGetSongIdByKeyword(name);
        LogMessage($"[ArcChart] {name} : {diff}");

        var s = _songInfoRaw.Songs.SongList.Find(x => x.Id == name);
        if (s is null)
            return $"未找到谱面：{name}";

        var d = s.Difficulties[diff];
        if (d is null)
            return $"未找到谱面：{name}";

        try
        {
            string path = $"Arcaea/previews/{name}_{diff}.jpg";
            FileInfo fi = new(BotUtil.CombinePath(path));
            if (!fi.Exists)
            {
                var (_, _title) = s.GetSongFontAndName(diff);

                AffRenderer r = new(BotUtil.CombinePath(s.GetPath() + $"{diff}.aff"))
                {
                    Title = _title,
                    Artist = s.Artist,
                    Charter = d.ChartDesigner,
                    ChartBpm = (float)s.BpmBase,
                    Side = s.Side,
                    Difficulty = diff,
                    Rating = GetRating(name, diff),
                    Notes = GetNotes(name, diff),
                };

                var bg = $"arcaea/assets/img/bg/{s.GetBg(diff)}.jpg";
                LogMessage("[Aff2Preview] " + bg);

                if (s.Side == 0)
                {
                    r.LoadResource(
                        BotUtil.CombinePath("arcaea/assets/img/note.png"),
                        BotUtil.CombinePath("arcaea/assets/img/note_hold.png"),
                        BotUtil.CombinePath("arcaea/assets/img/arc_body.png"),
                        BotUtil.CombinePath(bg),
                        BotUtil.CombinePath(s.GetCover(diff)));
                }
                else
                {
                    r.LoadResource(
                        BotUtil.CombinePath("arcaea/assets/img/note_dark.png"),
                        BotUtil.CombinePath("arcaea/assets/img/note_hold_dark.png"),
                        BotUtil.CombinePath("arcaea/assets/img/note_hold.png"),
                        BotUtil.CombinePath(bg),
                        BotUtil.CombinePath(s.GetCover(diff)));
                }

                r.Draw()?.SaveToJpg(BotUtil.CombinePath(path), 95);

                fi = new FileInfo(BotUtil.CombinePath(path));

                if (fi.Exists)
                    return new MessageBuilder(ImageChain.Create(path)).Build();
                else
                    return "[Aff2Preview] 未找到 Aff 文件或渲染出错。";
            }
            else
            {
                return new MessageBuilder(ImageChain.Create(path)).Build();
            }
        }
        catch (Exception ex)
        {
            BotLogger.LogE("ArcChart", $"{ex.Message}\n{ex.StackTrace}");
            return $"[Aff2Preview] 解析出错: {ex.Message}";
        }
    }
}
