﻿using System.Drawing;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using AimuBot.Core.Config;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;
using AimuBot.Modules.Arcaea.AuaJson;

using LunaUI.Layouts;

using Pinyin4net;
using Pinyin4net.Format;

namespace AimuBot.Modules;

[Module("杂项",
    Version = "1.0.0",
    Description = "杂项功能")]
internal class Misc : ModuleBase
{
    [Command("mmmm",
        Name = "生成猫猫语",
        Description = "将一段话按照拼音声调替换为*喵苗秒妙*。",
        Template = "/mmmm <content>\n/喵苗秒妙 <content>",
        Example = "/mmmm 喵",
        Alias = new string[] { "喵苗秒妙" },
        Category = "杂项",
        SendType = SendType.Send)]
    public MessageChain OnMMMM(BotMessage msg)
    {
        HanyuPinyinOutputFormat format = new()
        {
            ToneType = HanyuPinyinToneType.WITH_TONE_NUMBER,
            VCharType = HanyuPinyinVCharType.WITH_V,
            CaseType = HanyuPinyinCaseType.LOWERCASE
        };

        var cats = new[] { "喵", "喵", "苗", "秒", "妙", "喵~" };
        var rev = "";

        foreach (var c in msg.Content)
            try
            {
                var id = PinyinHelper.ToHanyuPinyinStringArray(c, format)[0];
                rev += cats[Convert.ToInt32(id[^1..])];
            }
            catch
            {
                rev += c;
            }

        return rev;
    }

    private long _guyLock = long.MaxValue;

    [Command("guy",
        Name = "生成Guy发言",
        Description = "生成Guy先生的Discord发言。",
        Template = "/guy <content>",
        Example = "/guy 616sb",
        CooldownType = CooldownType.Bot,
        CooldownSecond = 15,
        Matching = Matching.StartsWith,
        Level = RbacLevel.Normal,
        SendType = SendType.Send)]
    public MessageChain OnGuy(BotMessage msg)
    {
        var content = msg.Content;

        if (content.Length > 256)
            return "";

        if (new Regex(@"^\s+$").IsMatch(content))
            return "";

        if (BotUtil.Timestamp < _guyLock + 15 * 1000)
            return "";

        _guyLock = BotUtil.Timestamp;

        LunaUI.LunaUI ui = new(BotUtil.ResourcePath, "Generate/Guy.json");
        ui.GetNodeByPath<LuiText>("Text").Text = DateTime.Now.ToString("yyyy/MM/dd");
        ui.GetNodeByPath<LuiText>("Text1").Text = content;

        /*+= new List<string>()
        {
            "", "", " who cares"," no one cares",", no one cares", ", I don't care", " noone cares"
        }.Random();*/

        Image im = new Bitmap(ui.Root.Option.CanvasSize.Width, ui.Root.Option.CanvasSize.Height);
        using (var g = Graphics.FromImage(im))
        {
            Font f = new("微软雅黑", 40, FontStyle.Regular, GraphicsUnit.Pixel);
            var sf = g.MeasureString(content, f, 1200);
            var w = sf.Width > 380 ? ui.Root.Root.Size.Width + sf.Width - 380 + 20 : ui.Root.Root.Size.Width;
            var h = sf.Height > 55 ? ui.Root.Root.Size.Width + sf.Height - 480 : ui.Root.Root.Size.Height;
            ui.Root.Root.Size = new Size((int)w, (int)h);
            ui.GetNodeByPath<LuiColorLayer>("ColorLayer").Size =
                ui.Root.Root.Size with { Width = (int)w, Height = (int)h };

            ui.GetNodeByPath<LuiText>("Text1").Size = new Size((int)sf.Width + 50, (int)sf.Height);
        }

        ui.Render().SaveToJpg(BotUtil.CombinePath("Generate/Guy_gen.jpg"), 85);

        return new MessageBuilder(ImageChain.Create("Generate/Guy_gen.jpg")).Build();
    }

    [Command("roll",
        Name = "帮您选择",
        Description = "从几个选项中选择一个。",
        Template = "/roll <option>[< |,|，><option>]...",
        Example = "/roll fktx fkucn fk616\n/roll 肯德基，麦当劳，不吃",
        Category = "杂项",
        NeedSensitivityCheck = true,
        Matching = Matching.StartsWith,
        Level = RbacLevel.Normal,
        SendType = SendType.Send)]
    public MessageChain OnRoll(BotMessage msg)
    {
        var s = msg.Content.Split(',', '，', ' ').ToList().Random();
        if (s.IsNullOrEmpty())
            return "";

        var tip = new[]
        {
            "那我建议你选择<option>",
            "我觉得还是选<option>吧",
            "不如选<option>"
        };

        var op = new Random().Next(0, tip.Length);

        return tip[op].Replace("<option>", s);
    }

    [Command("https://github.com/",
        Name = "Github parser",
        Description = "将 Github url 链接解析为图片（适用于repo,issue等）",
        Template = "<Github_repo_url>",
        Example = "https://github.com/KonataDev/Konata.Core",
        Category = "杂项",
        Matching = Matching.StartsWithNoLeadChar,
        Level = RbacLevel.Normal,
        SendType = SendType.Send)]
    public async Task<MessageChain> OnCommandGithubParser(BotMessage msg)
    {
        // UrlDownload the page
        try
        {
            LogMessage(msg.Body);

            var bytes = await $"{msg.Body.TrimEnd('/')}.git".UrlDownload(true);

            var html = Encoding.UTF8.GetString(bytes);

            // Get meta data
            var metaData = html.GetMetaData("property");
            var imageMeta = metaData["og:image"];

            // Build message
            var image = await imageMeta.UrlDownload();
            await File.WriteAllBytesAsync(BotUtil.CombinePath("misc/gh.png"), image);
            return new MessageBuilder(ImageChain.Create("misc/gh.png")).Build();
        }
        catch (Exception e)
        {
            LogMessage("Not a repository link. \n" +
                       $"{e.Message}");
            return "";
        }
    }
}