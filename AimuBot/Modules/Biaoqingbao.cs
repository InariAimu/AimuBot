using System.Text;
using System.Text.RegularExpressions;

using AimuBot.Core.Config;
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

    private Dictionary<string, Dictionary<int, string>> _images = new();

    public override bool OnReload()
    {
        _nameAlias.CreateTable();

        _images.Clear();
        DirectoryInfo di = new(_imgPath);
        
        foreach (var d in di.GetDirectories())
        {
            var na = _nameAlias.GetAlias(d.Name);
            _imageAlias.TryAdd(d.Name, na?.ToList());

            Dictionary<int, string> f = new();

            var files = d.GetFiles();
            var maxIndex = 0;
            
            foreach (var file in files)
            {
                if (!new Regex(@"^\d+\.\w+$").IsMatch(file.Name)) continue;
                
                var fileIndex = Convert.ToInt32(file.Name.SubstringBeforeLast("."));
                f.Add(fileIndex, $"{d.Name}/{file.Name}");
                maxIndex = Math.Max(maxIndex, fileIndex);
            }

            maxIndex++;
            files = d.GetFiles();
            foreach (var file in files)
            {
                if (new Regex(@"^\d+\.\w+$").IsMatch(file.Name)) continue;
                
                var newPath = Path.Combine(file.DirectoryName, $"{maxIndex}{file.Extension}");
                File.Move(file.FullName, newPath);
                f.Add(maxIndex, $"{d.Name}/{maxIndex}{file.Extension}");
                maxIndex++;
            }

            _images.Add(d.Name, f);
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
        var c = msg.Content.Trim().ToLower();
        var content = c.SubstringBeforeLast(" ");

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

        var numStr = c.SubstringAfterLast(" ", "-1");
        if (imgDir.IsNullOrEmpty())
            return "";

        var num = -1;
        if (!numStr.IsNullOrEmpty()) num = Convert.ToInt32(numStr);

        return GetImage(imgDir, num);
    }

    private MessageChain GetImage(string directory, int id = -1)
    {
        LogMessage($"{directory}, {id}");

        var d = _images[directory];
        var c = d.Count;
        var im = (id < 0 || id >= c) ? d[BotUtil.Random.Next(0, c)] : d[id];

        return new MessageBuilder(ImageChain.Create(Path.Combine(_imgPath, im))).Build();
    }
}