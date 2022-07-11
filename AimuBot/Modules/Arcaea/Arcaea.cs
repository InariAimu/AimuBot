using System.Drawing;
using System.Text.RegularExpressions;

using AimuBot.Core.Config;
using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;
using AimuBot.Core.Extensions;
using AimuBot.Modules.Arcaea.AffTools.Aff2Preview;

namespace AimuBot.Modules.Arcaea;

[Module("Arcaea",
    Command = "ac",
    CommandDesc = "Arcaea 模块中大部分命令均以该命令起始。如果不指定任何参数，该功能的行为相当于 [Recent查询](#recent-查询)。",
    Description =
        "提供 Arcaea 查分，b30，谱面预览等功能。使用 `ac` 与其他 Bot 的 `arc`，`a` 等进行区分，避免一呼百应。同时适量增加操作成本，防止滥用。\n" +
        "**使用大部分功能之前，请先使用 `/ac bind` 绑定一个 arcaea id。**\n"+
        "::: warning 注意\nArcaea 是 Lowiro 的注册商标。商标是其各自所有者的财产。游戏材料的版权归 Lowiro 所有。Lowiro 没有认可也不对本网站或其内容负责。\n:::\n",
    Eula =
        "您应知悉，使用本模块进行 Arcaea 查分将违反 *Arcaea使用条款 3.2-4 和 3.2-6*，以及 *Arcaea二次创作管理条例*。\n" +
        "因使用本功能进行查分而造成的损失（包括但不限于账号被 lowiro 封禁、shadowban 等），Aimubot 开发组不予承担任何责任。",
    Privacy =
        "使用查分将默认您允许 Aimubot 收集/记录关于您的使用记录，包括但不限于 Arcaea 用户名、游玩记录等。\n" +
        "您的数据将用于历史 ptt 显示、最高分记录显示、推分建议等功能。\n" +
        "我们将使用行业安全标准来保存数据，并不会提供给任何第三方。")]
public partial class Arcaea : ModuleBase
{
    private ArcaeaDatabase _db = null!;
    private readonly ArcaeaNameAlias _arcaeaNameAlias = new();

    private ArcaeaSongListRawWrapper _songInfoRaw = new();

    public ArcaeaDatabase GetDB() => _db;

    public override bool OnInit()
    {
        _db = new ArcaeaDatabase();
        _db.CreateTables();
        return true;
    }

    public override bool OnReload()
    {
        _songInfoRaw.LoadFromSlst();
        return true;
    }

    [Command("acs",
        Name = "保存分数",
        Description = "在不查询或无法查询成绩时保存分数",
        BlocksBefore = new[]
            { "::: warning 注意\n仅用于无法查分时在 bot 数据库中手动保存您的成绩，请谨慎使用。\n:::" },
        Template = "/acs <song_name> [difficulty=ftr] <score>",
        Example = "/acs testify byd 10002221",
        Category = "Arcaea",
        State = State.Test,
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public MessageChain OnStoreLocalScore(BotMessage msg)
    {
        var content = msg.Content;
        var paramArr = content.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        ScoreDesc sd = new()
        {
            Type = 1,
            Difficulty = 2
        };

        var m = new Regex(@"\s\d{8}").Match(content);
        if (!m.Success)
            return "";

        var scoreStr = m.Value;
        content = content.Replace(scoreStr, "");

        var md = new Regex(@"\s(pst|prs|ftr|byd)").Match(content);
        if (m.Success)
        {
            var diffStr = m.Value;
            content = content.Replace(diffStr, "");
            sd.Difficulty = diffStr.ToLower() switch
            {
                "pst" => 0,
                "prs" => 1,
                "ftr" => 2,
                "byd" => 3,
                _     => 2
            };
        }

        var songId = TryGetSongIdByKeyword(content.Trim());

        var song = _songInfoRaw.Songs.SongList.Find(x => x.Id == songId);
        if (song is null)
            return "";

        var rating_f = GetRating(songId, sd.Difficulty);

        sd.time = BotUtil.Timestamp;

        //db.SaveScore(sd);
        return "保存成功";
    }

    [Command("ac bind",
        Name = "绑定 Arcaea",
        Description = "绑定 Arcaea Id 或 Name（推荐使用 9 位 Arcaea 数字 id）",
        Template = "/ac bind <arc_id>",
        Example = "/ac bind ToasterKoishi\n/ac bind 000000001",
        Category = "Arcaea",
        Matching = Matching.StartsWith,
        Level = RbacLevel.Normal,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnBind(BotMessage msg)
    {
        var content = msg.Content;
        LogMessage($"Bind: {content} -> {msg.SenderId}");

        Regex arcIdRegex = new(@"^\d{9}$");
        Regex arcNameRegex = new(@"^[A-Za-z\d_]+$");

        if (arcIdRegex.IsMatch(content) || arcNameRegex.IsMatch(content))
        {
            var response = await GetUserRecent(content);
            if (response.Status < 0)
                return $"错误：{response.Status} {response.Message}";

            var (succ, bindInfo) = _db.GetObject<BindInfoDesc>(
                "arc_id = $arc_id",
                new Dictionary<string, object> { { "$arc_id", response.Content.AccountInfo.Code } }
            );
            if (succ)
            {
                bindInfo.BindType = 0;
                bindInfo.QqId = msg.SenderId;
                bindInfo.ArcId = content;
                bindInfo.Name = response.Content.AccountInfo.Name;
            }
            else
            {
                bindInfo = new BindInfoDesc
                {
                    BindType = 0,
                    QqId = msg.SenderId,
                    ArcId = response.Content.AccountInfo.Code,
                    Name = response.Content.AccountInfo.Name,
                    B30Type = 2,
                    RecentType = 0
                };
            }

            _db.SaveObject(bindInfo);
            return
                $"已绑定 {response.Content.AccountInfo.Name}/{response.Content.AccountInfo.Code} ({response.Content.AccountInfo.PttText})";
        }
        else
        {
            return "格式不正确：请使用Arcaea数字id或者角色名进行绑定";
        }
    }

    [Command("ac chart",
        Name = "谱面预览",
        Description = "获取指定谱面的平面预览图。",
        BlocksBefore = new[] { "::: warning 注意\n`Final Verdict` 中的一些谱面目前渲染不正常，正在修复中。\n:::" },
        Template = "/ac chart <song_name> [difficulty=ftr]",
        Example = "/ac chart 猫魔王 byd\n/ac chart dropdead pst\n/ac chart ifi",
        Category = "Arcaea",
        State = State.Test,
        Matching = Matching.StartsWith,
        Level = RbacLevel.Normal,
        SendType = SendType.Send)]
    public MessageChain OnChart(BotMessage msg)
    {
        var content = msg.Content;
        var diff = 2;
        var name = "";
        if (content.Length < 3)
        {
            name = content;
        }
        else
        {
            var _difficulty = content[^3..];
            (diff, name) = _difficulty.ToLower() switch
            {
                "pst" => (0, content[..^4]),
                "prs" => (1, content[..^4]),
                "ftr" => (2, content[..^4]),
                "byd" => (3, content[..^4]),
                _     => (2, content)
            };
        }

        name = name.Trim();
        name = TryGetSongIdByKeyword(name);
        LogMessage($"[ArcChart] {name} : {diff}");

        var s = _songInfoRaw.Songs.SongList.Find(x => x.Id == name);
        if (s is null)
            return "";

        var d = s.Difficulties[diff];

        try
        {
            var path = $"Arcaea/previews/{name}_{diff}.jpg";
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
                    IsMirror = false
                };

                var bg = $"arcaea/assets/img/bg/{s.GetBg(diff)}.jpg";
                LogMessage("[Aff2Preview] " + bg);

                if (s.Side == 0)
                    r.LoadResource(
                        BotUtil.CombinePath("arcaea/assets/img/note.png"),
                        BotUtil.CombinePath("arcaea/assets/img/note_hold.png"),
                        BotUtil.CombinePath("arcaea/assets/img/arc_body.png"),
                        BotUtil.CombinePath(bg),
                        BotUtil.CombinePath(s.GetCover(diff)));
                else
                    r.LoadResource(
                        BotUtil.CombinePath("arcaea/assets/img/note_dark.png"),
                        BotUtil.CombinePath("arcaea/assets/img/note_hold_dark.png"),
                        BotUtil.CombinePath("arcaea/assets/img/note_hold.png"),
                        BotUtil.CombinePath(bg),
                        BotUtil.CombinePath(s.GetCover(diff)));

                (r.Draw()?.InnerImage as Image)?.SaveToJpg(BotUtil.CombinePath(path), 95);

                fi = new FileInfo(BotUtil.CombinePath(path));

                if (fi.Exists)
                    return new MessageBuilder(ImageChain.Create(path)).Build();
                else
                    BotLogger.LogE("ArcChart", "未找到 Aff 文件或渲染出错。");

                //return "[Aff2Preview] 未找到 Aff 文件或渲染出错。";
                return "";
            }
            else
            {
                return new MessageBuilder(ImageChain.Create(path)).Build();
            }
        }
        catch (Exception ex)
        {
            BotLogger.LogE("ArcChart", $"{ex.Message}\n{ex.StackTrace}");

            //return $"[Aff2Preview] 解析出错: {ex.Message}";
            return "";
        }
    }
}