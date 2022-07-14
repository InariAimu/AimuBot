using System.Drawing;
using System.Net;
using System.Net.Security;
using System.Xml;

using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;

namespace AimuBot.Modules;

[Module("WolframAlpha",
    Version = "1.0.1",
    Description = "WolframAlpha 查询（请使用英语）")]
internal class WolframAlpha : ModuleBase
{
    [Config("api_key", DefaultValue = "XXXXXX-XXXXXXXXXX")]
    private string _apiKey = null!;

    [Command("wa",
        Name = "WolframAlpha查询",
        Description = "进行一次 [WolframAlpha](https://www.wolframalpha.com/) 查询（请使用英语）。视网络情况，查询可能需要 5~30 秒，请勿重复查询。",
        Template = "/wa <query_content>",
        BlocksBefore = new[]
        {
            "::: tip\n示例可以从 [Examples by Topic](https://www.wolframalpha.com/examples/) 获取。\n:::",
            "::: warning 注意\n由于接口为 WolframAlpha Non-commercial API，因此查询结果禁止任何形式的商业使用。\n:::",
            "::: danger 警告\n**绝对禁止**查询任何涉及人物、政治相关内容。AimuBot 已内置内容审核，查询上述内容一经发现将**永久封禁您的 Bot 使用权限**。\n:::",
        },
        NekoBoxExample =
            "{ position: 'right', msg: '/wa heart surface' }," +
            "{ position: 'left', chain: [{ reply: '/wa heart surface' }, { img: '/images/WolframAlpha/1.webp' }] }," +
            "{ position: 'right', msg: '/wa mass of sun' }," +
            "{ position: 'left', chain: [{ reply: '/wa mass of sun' }, { img: '/images/WolframAlpha/2.webp' }] }," +
            "{ position: 'right', msg: '/wa #1f1e33' }," +
            "{ position: 'left', chain: [{ reply: '/wa #1f1e33' }, { img: '/images/WolframAlpha/3.webp' }] },",
        NeedSensitivityCheck = true,
        CooldownType = CooldownType.User,
        CooldownSecond = 10,
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnWolframAlphaQuery(BotMessage msg)
    {
        var token = _apiKey;
        var param = $"input={WebUtility.UrlEncode(msg.Content)}&appid={token}";

        LogMessage(param);

        var s = await GetString("http://api.wolframalpha.com/v2/query?" + param);

        XmlDocument xml = new();
        xml.LoadXml(s);
        var success = xml.SelectSingleNode("/queryresult").Attributes["success"].Value == "true";
        var error = xml.SelectSingleNode("/queryresult").Attributes["error"].Value == "true";

        if (!success || error) return "Wolfram Alpha query error occured.";

        var imgNodes = xml.SelectNodes("/queryresult/pod");
        var podCount = 0;
        List<string> titles = new();
        List<Task> downloadImgTasks = new();

        foreach (XmlNode node in imgNodes)
        {
            var title = node.Attributes["title"].Value;
            var imgUrl = node.InnerXml.GetSandwichedText("img src=\"", "\"").Replace("amp;", "");

            downloadImgTasks.Add(DownloadFileFrom(imgUrl, $"WolframAlpha/{podCount}.gif"));

            titles.Add(title);
            LogMessage(title + "," + podCount);
            podCount++;
        }

        Task.WaitAll(downloadImgTasks.ToArray());

        Font? font = new("exo", 12f, FontStyle.Bold);
        SolidBrush? textBrush = new(Color.White);
        unchecked
        {
            textBrush.Color = Color.FromArgb((int)0xffff7d00);
        }

        SolidBrush? shadeBrush = new(Color.LightGray);
        unchecked
        {
            shadeBrush.Color = Color.FromArgb((int)0xfff7f7f7);
        }

        List<Image> images = new();
        var width = 0;
        var height = 0;
        for (var t = 0; t < podCount; t++)
        {
            var im = Image.FromFile(BotUtil.CombinePath($@"WolframAlpha\{t}.gif"));
            images.Add(im);
            width = Math.Max(width, im.Width);
            height += im.Height + 35;
        }

        if (width < 340)
            width = 340;

        LogMessage($"Total img size: {width},{height}");

        var dh = 5;
        Image image = new Bitmap(width + 20, height + 40);

        using (var g = Graphics.FromImage(image))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.Clear(Color.White);
            for (var t = 0; t < podCount; t++)
            {
                g.FillRectangle(shadeBrush, -1, dh, width + 20 + 2, 25);
                g.DrawString(titles[t], font, textBrush, new PointF(5, dh));
                dh += 30;
                g.DrawImageUnscaled(images[t], 10, dh);
                dh += images[t].Height + 5;
            }

            const string rightFooter = "Wolfram Alpha Non-commercial API / AimuBot";
            g.FillRectangle(shadeBrush, 0, height + 40 - 20, width + 20, 20);
            var sz = g.MeasureString(rightFooter, font);

            textBrush.Color = Color.DarkRed;
            g.DrawString(rightFooter, font, textBrush,
                width + 30 - sz.Width - 10, height + 40 - 20, new StringFormat(StringFormatFlags.NoWrap));

            BotUtil.SaveImageToJpg(image, "WolframAlpha/result.jpg", 95);
        }

        image.Dispose();
        images.ForEach(i => i.Dispose());
        images.Clear();

        return new MessageBuilder(ImageChain.Create("WolframAlpha/result.jpg")).Build();
    }

    private async Task<string> GetString(string url)
    {
        var html = "";
        ServicePointManager.ServerCertificateValidationCallback = (obj, cert, chain, errs) => true;
        using HttpClient hc = new();
        html = await hc.GetStringAsync(url);
        return html;
    }

    private async Task<byte[]> GetByteArray(string url)
    {
        ServicePointManager.ServerCertificateValidationCallback = (obj, cert, chain, errs) => true;
        using HttpClient hc = new();
        return await hc.GetByteArrayAsync(url);
    }

    private async Task DownloadFileFrom(string url, string relativePath)
    {
        var buff = await GetByteArray(url);
        await File.WriteAllBytesAsync(BotUtil.CombinePath(relativePath), buff);
    }
}