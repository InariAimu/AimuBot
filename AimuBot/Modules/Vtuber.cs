using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using AimuBot.Core.Config;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;

using Microsoft.CodeAnalysis.CSharp.Syntax;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AimuBot.Modules;

[Module("Vtuber",
    Version = "1.0.0",
    Description = "管人模块")]
internal class Vtuber : ModuleBase
{
    [Command("来点d看",
        Name = "来点d看",
        Description = "从正在直播的虚拟主播直播间中随机选择一个。",
        BlocksBefore = new[]
        {
            "::: warning 注意\n将会从 bilibili 直播中随机选择一个，可能不一定对您的胃口。请谨慎使用。\n:::",
        },
        Template = "/来点d看",
        NekoBoxExample = 
            "{ position: 'right', msg: '/来点d看' },"+
            "{ position: 'left', chain: [{ reply: '/来点d看' }, { img: '/images/Vtuber/example.webp' }, { msg: '标题: unpacking\\nUP: 猫雷NyaRu_Official\\nhttps://live.bilibili.com/22676119' }] },",
        State = State.Developing,
        Matching = Matching.Exact,
        SendType = SendType.Reply)]
    public MessageChain OnGetRandomV(BotMessage msg)
    {
        return "";
    }

    [Command("每日猴报",
        Name = "每日猴报",
        Description = "获取 24 小时内猴楼 `Hololive` 的开播情况。",
        BlocksBefore = new[]
        {
            "::: warning 注意\n请勿滥用。\n\n使用前请确保您知道自己在做什么，并且确保不会打扰到群聊内其他成员。\n:::",
            "::: danger 注意\n由于猴楼炸箱，本命令已停止服务。\n:::"
        },
        Template = "/每日猴报",
        NekoBoxExample =
            "{ position: 'right', msg: '/每日猴报' }," +
            "{ position: 'left', chain: [{ reply: '/每日猴报' }, { msg: '今日猴报：\\n06:30 赤井はあと ❤, 09:30 緋崎ガンマ, 19:00 ロボ子さん 🤖, 20:00 大空スバル 🚑, \\n20:00 白銀ノエル ⚔, 21:00 夜十神封魔, ' }] },",
        State = State.Disabled,
        CooldownType = CooldownType.Group,
        CooldownSecond = 600,
        Matching = Matching.Exact,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnDailyHolo(BotMessage msg)
    {
        LogMessage("1");
        
        var result = new StringBuilder();

        var rev = await "https://schedule.hololive.tv/simple".UrlDownload();
        var html = Encoding.UTF8.GetString(rev);
        html = WebUtility.HtmlDecode(html);

        var list = new Dictionary<int, List<string>>();
        var state = "day";
        var date = 0;

        while (state != "end")
        {
            var datePart = html.IndexOf("<div class=\"holodule navbar-text\"", StringComparison.Ordinal);
            var timePart = html.IndexOf("href=\"https://www.youtube.com", StringComparison.Ordinal);

            if (datePart == -1 && timePart == -1)
                break;

            state = (datePart, timePart) switch
            {
                (_, -1)               => "day",
                (-1, _)               => "time",
                var (a, b) when a < b => "day",
                _                     => "time",
            };

            LogMessage($"{datePart},{timePart},{state}");
            
            if (state == "day")
            {
                var s = html.GetSandwichedText("<div class=\"holodule navbar-text\"", "</div>");
                if (s == "") continue;

                var tp = $"<div class=\"holodule navbar-text\"{s}</div>";
                html = html.SubstringAfter(tp).ReplaceFirst(tp, "");
                var dateText = s.GetSandwichedText(">", "")
                    .Replace("\n", "")
                    .Replace(" ", "")
                    .Trim();
                LogMessage(dateText);
                var m = Convert.ToInt32(dateText[0..2]);
                var d = Convert.ToInt32(dateText[3..5]);
                date = m * 100 + d;
                list[date] = new();
            }
            else
            {
                if (date <= 0) continue;

                var dd = html.GetSandwichedText("href=\"https://www.youtube.com", "img");
                if (dd == "") continue;

                var tp = "href=\"https://www.youtube.com" + dd + "img";
                html = html.SubstringAfter(tp).ReplaceFirst(tp, "");
                var ddTime = dd.GetSandwichedText(">", "<")
                    .Replace("\n", "")
                    .Replace(new Regex(@"\s+"), " ")
                    .SubstringBefore("&#")
                    .Trim();
                LogMessage(ddTime);
                list[date]?.Add(ddTime);
            }
        }

        var dt = DateTime.Now;
        var hrNow = dt.Hour;
        var dTime = dt.Month * 100 + dt.Day;

        if (!list.ContainsKey(dTime))
            return "今天没有猴楼看。";

        LogMessage(list[dTime].Count);
        
        var i = 0;
        foreach (var sd in list[dTime])
        {
            LogMessage(sd);
            var sTime = sd.Split(' ')[0];
            var hm = sTime.Split(':');
            var hr = Convert.ToInt32(hm[0]);

            if (hr <= hrNow) continue;

            result.Append($"{sd}，");
            i++;
            if (i <= 3) continue;

            result.AppendLine();
            i = 0;
        }

        return result.ToString();
    }

    [Command("嘉然今天吃什么",
        Name = "嘉然今天吃什么",
        Description = "随机获取一段 `Asoul` 发病小作文。",
        BlocksBefore = new[]
        {
            "::: warning 注意\n可能含有令人反感或不适的内容。\n\n请勿滥用。\n\n使用前请确保您知道自己在做什么，并且确保不会打扰到群聊内其他成员。\n\n" +
            "小作文获取自 https://asoul.icu/v/articles，AimuBot 开发组对其中的任何内容并不知情，也不会对其中任何内容负责。\n:::",
            "::: danger 注意\n由于 https://asoul.icu/v/articles 无法访问，本命令已停止服务。\n:::"
        },
        Template = "/嘉然今天吃什么",
        NekoBoxExample =
            "{ position: 'right', msg: '/嘉然今天吃什么' }," +
            "{ position: 'left', msg: '鼠鼠\\n\\n鼠鼠的朋友有很多，住在东边的小鱼，住在南边的小鹿，住在西边的蝴蝶，住在北边的小鸟。他们带鼠鼠在蔚蓝色的深海里与水母共游，在碧绿的麋鹿森林里喝清晨的露水，在热闹的雨林里穿梭玩耍，在广袤的天空中肆意飞翔。\\n\\n" +
            "鼠鼠住在灯火阑珊的城市，可鼠鼠知道这里没有一处属于鼠鼠，鼠鼠穿过川流不息的街道，狂奔着回到自己阴湿黑暗的下水道，强烈的自卑之情让鼠鼠无法呼吸，鼠鼠依靠在下水道缝隙边，因为嘉然小姐总会路过这里。\\n\\n" +
            "草莓加奶油加花香，是嘉然小姐的味道。风铃加口琴加奶糖，是嘉然小姐的声音。节奏加音乐加快乐，是嘉然小姐的脚步。“她来了”，鼠鼠将头小心地探出缝隙，嘉然小姐径直走过吵闹的人群，来到下水道缝隙边，端着草莓蛋糕，缓缓放在洞口，一阵风铃似的声音响起“嘉心糖，来吃然然的草莓蛋糕吧。”嘉然小姐温柔的看着它，鼠鼠不知为何突然流下了眼泪，无法挪动脚步。嘉然小姐好像很失落，眉头轻轻皱起，“你不喜欢然然吗……”，鼠鼠急忙辩解，可憋红了脸一句话也说不出，“猫咪已经被然然关在笼子里了，嘉心糖不用害怕！”嘉然小姐关切的看着鼠鼠，鼠鼠似乎要溺亡在她蔚蓝色的双眸里，慢慢走向那块草莓蛋糕，小口品尝着。嘉然小姐伸出小小的手，似乎想捧起鼠鼠。鼠鼠好像被雷击中一般，猛然跳起，飞奔回下水道，它拼命喊着：“我又脏又臭还很丑陋，嘉然小姐不会喜欢我的！嘉然小姐不会喜欢我的！”嘉然小姐表情立刻转为严肃，认真的看着鼠鼠道“我不许你这么说自己！”她的眼中好像也盈满了泪水“我们嘉心糖…都是很厉害的人！" +
            "' },",
        State = State.Normal,
        Level = RbacLevel.Super,
        Matching = Matching.Exact,
        SendType = SendType.Send)]
    public async Task<MessageChain> OnSmallEssay(BotMessage msg)
    {
        var url =
            $"https://asoul.icu/v/articles?pageNum={BotUtil.Random.Next(0, 4)}&pageSize=48&kw=&tags=%E5%98%89%E7%84%B6";
        LogMessage(url);
        
        var rev = await url.UrlDownload(false);
        var html = Encoding.UTF8.GetString(rev);
        html = html.Trim();
        
        var page = JsonConvert.DeserializeObject<JiaranPage>(html);

        var articles = page.Articles.Where(x => !x.Tags.Contains("R18"));
        if (!articles.Any())
            return "未找到小作文。";

        var articleUrl =
            $"https://asoul.icu/v/articles/{WebUtility.UrlEncode(articles.Random().Title)}";
        LogMessage(articleUrl);

        rev = await articleUrl.UrlDownload(false);
        html = Encoding.UTF8.GetString(rev);
        html = html
            .Replace("<html>", "")
            .Replace("</html>", "")
            .Replace("<head>", "")
            .Replace("</head>", "")
            .Replace("<body>", "")
            .Replace("</body>", "")
            .Trim();

        var article = JsonConvert.DeserializeObject<JiaranInfoDetail>(html);
        var s = article.HtmlContent
            .Replace("</p>", "\n")
            .Replace("<p>", "")
            .Replace("<br>", "")
            .Replace("<em>", "")
            .Replace("</em>", "")
            .Replace("\n\n", "\n")
            .Trim();
        
        BotLogger.LogI("xzw", $"[{article.Title}]\n" + s);
        //return $"[{article.Title}]\n" + s;
        return "";
    }

    [Serializable]
    private class JiaranPage
    {
        [JsonProperty("count")]public long Count { get; set; } 
        [JsonProperty("info")]public string Info { get; set; } 
        [JsonProperty("articles")]public List<JiaranInfo> Articles { get; set; } 
    }

    [Serializable]
    private class JiaranInfo
    {
        [JsonProperty("_id")]public string Id { get; set; } 
        [JsonProperty("title")]public string Title { get; set; } 
        [JsonProperty("author")]public string Author { get; set; } 
        [JsonProperty("plainContent")]public string PlainContent { get; set; } 
        [JsonProperty("submissionTime")]public long SubmissionTime { get; set; } 
        [JsonProperty("tags")]public List<string> Tags { get; set; } 
    }
    
    [Serializable]
    private class JiaranInfoDetail
    {
        [JsonProperty("_id")]public string Id { get; set; } 
        [JsonProperty("title")]public string Title { get; set; } 
        [JsonProperty("author")]public string Author { get; set; } 
        [JsonProperty("htmlContent")]public string HtmlContent { get; set; } 
        [JsonProperty("submissionTime")]public long SubmissionTime { get; set; } 
        [JsonProperty("tags")]public List<string> Tags { get; set; } 
    }
}