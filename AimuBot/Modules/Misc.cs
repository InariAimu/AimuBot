using System.Drawing;

using AimuBot.Core.Bot;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;
using LunaUI.Layouts;

using Pinyin4net;
using Pinyin4net.Format;

namespace AimuBot.Modules;

[Module(nameof(Misc),
    Version = "1.0.0",
    Description = "杂项功能")]
internal class Misc : ModuleBase
{
    [Command("mmmm",
        Name = "生成猫猫语",
        Description = "生成猫猫语",
        Tip = "/mmmm <content>",
        Example = "/mmmm 喵",
        Alias = new string[] { "喵苗秒妙" },
        Category = "杂项",
        SendType = SendType.Send)]
    public MessageChain OnMMMM(BotMessage msg)
    {
        HanyuPinyinOutputFormat format = new();
        format.ToneType = HanyuPinyinToneType.WITH_TONE_NUMBER;
        format.VCharType = HanyuPinyinVCharType.WITH_V;
        format.CaseType = HanyuPinyinCaseType.LOWERCASE;

        string[] cats = new string[] { "喵", "喵", "苗", "秒", "妙", "喵~" };

        string rev = "";

        foreach (char c in msg.Content)
        {
            try
            {
                string? id = PinyinHelper.ToHanyuPinyinStringArray(c, format)[0];
                rev += cats[Convert.ToInt32(id[^1..])];
            }
            catch
            {
                rev += c;
            }
        }

        return rev;
    }

    long guy_lock = long.MaxValue;

    [Command("guy",
        Name = "生成Guy发言",
        Description = "生成Guy先生的Discord发言。",
        Tip = "/guy <content>",
        Example = "/guy 616sb",
        CooldownType = CooldownType.Bot,
        CooldownSecond = 15,
        Matching = Matching.StartsWith,
        Level = RBACLevel.Normal,
        SendType = SendType.Send)]
    public MessageChain OnGuy(BotMessage msg)
    {
        string? content = msg.Content;

        if (BotUtil.Timestamp < guy_lock + 15 * 1000)
            return "";

        guy_lock = BotUtil.Timestamp;

        LunaUI.LunaUI ui = new(BotUtil.ResourcePath, "Generate/Guy.json");
        ui.GetNodeByPath<LuiText>("Text").Text = DateTime.Now.ToString("yyyy/MM/dd");
        ui.GetNodeByPath<LuiText>("Text1").Text = content;

        /*+= new List<string>()
        {
            "", "", " who cares"," no one cares",", no one cares", ", I don't care", " noone cares"
        }.Random();*/

        Image im = new Bitmap(ui.Root.Option.CanvasSize.Width, ui.Root.Option.CanvasSize.Height);
        using (Graphics g = Graphics.FromImage(im))
        {
            Font f = new("微软雅黑", 40, FontStyle.Regular, GraphicsUnit.Pixel);
            var sf = g.MeasureString(content, f);
            float w = sf.Width > 380 ? ui.Root.Root.Size.Width + sf.Width - 380 : ui.Root.Root.Size.Width;
            ui.Root.Root.Size = new Size((int)w, ui.Root.Root.Size.Height);
            ui.GetNodeByPath<LuiColorLayer>("ColorLayer").Size = new Size((int)w, ui.Root.Root.Size.Height);

            ui.GetNodeByPath<LuiText>("Text1").Size = new((int)sf.Width + 50, 55);
        }

        var origin = ui.Render();

        string? path = "Generate/Guy_gen.jpg";
        origin.SaveToJpg(BotUtil.CombinePath(path), 85);

        return new MessageBuilder(ImageChain.Create(path)).Build();
    }

    [Command("roll",
        Name = "帮您选择",
        Description = "从几个选项中选择一个。",
        Tip = "/roll <option>[< |,|，><option>]...",
        Example = "/roll fktx fkucn fk616\n/roll 肯德基，麦当劳，不吃",
        Category = "杂项",
        NeedSensitivityCheck = true,
        Matching = Matching.StartsWith,
        Level = RBACLevel.Normal,
        SendType = SendType.Send)]
    public MessageChain OnRoll(BotMessage msg)
    {
        List<string> options = msg.Content.Split(',', '，', ' ').ToList();
        string? s = options.Random();
        if (s.IsNullOrEmpty())
            return "";

        List<string> tip = new List<string>() { "那我建议你选择", "", "那就", "吧", "不如选", "", "为什么不", "呢" };

        int op = new Random().Next(0, tip.Count / 2);
        op *= 2;

        return tip[op] + s + tip[op + 1];
    }

}
