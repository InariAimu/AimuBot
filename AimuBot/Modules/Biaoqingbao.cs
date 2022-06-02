using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;

using Newtonsoft.Json;

namespace AimuBot.Modules;

[Module("表情包",
    Command = "")]
internal class Biaoqingbao : ModuleBase
{
    private readonly string _confFile = BotUtil.CombinePath("表情包/NameAlias.jsom");

    private Dictionary<string, List<string>> _imageAlias = new();
    private readonly string _imgPath = BotUtil.CombinePath("表情包/");

    public override bool OnReload()
    {
        if (File.Exists(_confFile))
        {
            _imageAlias = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(_confFile));
            DirectoryInfo di = new(_imgPath);
            foreach (var d in di.GetDirectories())
                if (!_imageAlias.ContainsKey(d.Name))
                    _imageAlias.Add(d.Name, new List<string>());
        }
        else
        {
            DirectoryInfo di = new(_imgPath);
            foreach (var d in di.GetDirectories()) _imageAlias.Add(d.Name, new List<string>());
            File.WriteAllText(_confFile, JsonConvert.SerializeObject(_imageAlias));
        }

        return true;
    }

    [Command("",
        Name = "获取指定表情包图片",
        Description = "获取指定表情包图片",
        Tip = "/<name> [id]",
        Example = "/care\n/care 10",
        Matching = Matching.Any,
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
        DirectoryInfo di = new(Path.Combine(_imgPath, directory));
        var files = di.GetFiles();
        if (id < 0 || id >= files.Length)
        {
            var im = files[id];
            return new MessageBuilder(ImageChain.Create(Path.Combine(_imgPath, directory, im.Name))).Build();
        }
        else
        {
            var im = files.ToList().Random();
            return new MessageBuilder(ImageChain.Create(Path.Combine(_imgPath, directory, im.Name))).Build();
        }
    }
}