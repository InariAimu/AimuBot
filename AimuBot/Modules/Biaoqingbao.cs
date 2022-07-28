using System.Text;

using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;
using AimuBot.Data;

using JetBrains.Annotations;

using Newtonsoft.Json;

namespace AimuBot.Modules;

internal class BqbNameAlias : NameAliasSystem
{
    public BqbNameAlias() : base("biaoqingbao")
    {
    }
}

[Module("表情包",
    Command = "")]
internal class Biaoqingbao : ModuleBase
{
    private readonly BqbNameAlias _nameAlias = new();

    private Dictionary<string, List<string>?> _imageAlias = new();
    private readonly string _imgPath = BotUtil.CombinePath("表情包/");

    public override bool OnReload()
    {
        _nameAlias.CreateTable();

        DirectoryInfo di = new(_imgPath);
        foreach (var d in di.GetDirectories())
        {
            var na = _nameAlias.GetAlias(d.Name);
            _imageAlias.TryAdd(d.Name, na?.ToList());
        }

        return true;
    }

    [UsedImplicitly]
    public string OnRequestImage_Doc()
    {
        StringBuilder sb = new();
        sb.AppendLine("可用表情包列表：（通过名称和别名均可获取）");
        sb.AppendLine();
        sb.AppendLine("| 名称 | 别名 |");
        sb.AppendLine("|:-----|:-----|");
        foreach (var (k, v) in _imageAlias)
        {
            var s = v is null ? string.Empty : string.Join(", ", v);
            sb.AppendLine($"| {k} | {s} |");
        }

        sb.AppendLine();
        return sb.ToString();
    }

    [Command("",
        Name = "表情包",
        Description = "获取随机或指定的表情包图片。",
        Template = "/<name> [id]",
        DescCustomFunc = "OnRequestImage_Doc",
        BlocksBefore = new[]
        {
            "::: tip 注意\n表情包功能内图片收集自网络；其版权归各自作者所有。有鉴于这些图片：\n\n- 公开流通于网络\n- 非盈利/非商业性使用\n\n" +
            "故在 AimuBot 中提供用于表情包功能。如您将其用于其他用途可能构成侵犯著作权。 AimuBot 开发组对您的行为不予承担任何责任。\n:::"
        },
        NekoBoxExample =
            "{ position: 'right', msg: '/arcaea' }," +
            "{ position: 'left', chain: [{ img: '/images/Biaoqingbao/10.jpg' },] }," +
            "{ position: 'right', msg: '/llmmt 13' }," +
            "{ position: 'left', chain: [{ img: '/images/Biaoqingbao/13.jpg' },] },",
        Matching = Matching.AnyWithLeadChar,
        CooldownType = CooldownType.Group,
        CooldownSecond = 5,
        SendType = SendType.Send)]
    public MessageChain OnRequestImage(BotMessage msg)
    {
        var content = msg.Content.Trim().ToLower().SubstringBeforeLast(" ");
        var numStr = content.SubstringAfterLast(" ");
        var num = -1;
        if (!numStr.IsNullOrEmpty()) num = Convert.ToInt32(num);

        var imgDir = "";
        foreach (var (k, v) in _imageAlias)
        {
            if (v is null)
                continue;

            if (k == content)
            {
                imgDir = k;
                break;
            }

            if (v.Any() && v.Contains(content))
            {
                imgDir = k;
                break;
            }
        }

        return !imgDir.IsNullOrEmpty() ? GetImage(imgDir, num) : "";
    }

    private MessageChain GetImage(string directory, int id = -1)
    {
        LogMessage($"{directory}, {id}");
        DirectoryInfo di = new(Path.Combine(_imgPath, directory));
        var files = di.GetFiles();

        var im = (id < 0 || id >= files.Length) ? files.ToList().Random() : files[id];
        return new MessageBuilder(ImageChain.Create(Path.Combine(_imgPath, directory, im.Name))).Build();
    }
}