
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
    Description = "WolframAlpha查询（请使用英语）")]
internal class WolframAlpha : ModuleBase
{
    [Config("api_key", DefaultValue = "XXXXXX-XXXXXXXXXX")]
    private string _apiKey = null!;

    [Command("wa",
        Name = "WolframAlpha查询",
        Description = "WolframAlpha查询（请使用英语）",
        Tip = "/wa <query_content>",
        Example = "/wa cat\n/wa sin(x)dx\n/wa mass of earth",
        NeedSensitivityCheck = true,
        CooldownType = CooldownType.User,
        CooldownSecond = 10,
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnWolframAlphaQuery(BotMessage msg)
    {
        string token = _apiKey;
        string? param = $"input={WebUtility.UrlEncode(msg.Content)}&appid={token}";

        LogMessage(param);

        string? s = await GetString("http://api.wolframalpha.com/v2/query?" + param);

        XmlDocument xml = new();
        xml.LoadXml(s);
        bool success = xml.SelectSingleNode("/queryresult").Attributes["success"].Value == "true";
        bool error = xml.SelectSingleNode("/queryresult").Attributes["error"].Value == "true";

        if (!success || error)
        {
            return "Wolfram Alpha query error occured.";
        }

        var imgNodes = xml.SelectNodes("/queryresult/pod");
        int podCount = 0;
        List<string> titles = new();
        List<Task> downloadImgTasks = new();
        foreach (XmlNode node in imgNodes)
        {
            string? title = node.Attributes["title"].Value;
            string img_url = node.InnerXml.GetSandwichedText("img src=\"", "\"").Replace("amp;", "");

            downloadImgTasks.Add(DownloadFileFrom(img_url, $@"WolframAlpha\{podCount}.gif"));

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
        int width = 0;
        int height = 0;
        for (int t = 0; t < podCount; t++)
        {
            Image? im = Image.FromFile(BotUtil.CombinePath($@"WolframAlpha\{t}.gif"));
            images.Add(im);
            width = Math.Max(width, im.Width);
            height += im.Height + 35;
        }
        if (width < 340)
            width = 340;

        LogMessage($"Total img size: {width},{height}");

        int dh = 5;
        Image image = new Bitmap(width + 20, height + 40);

        using (Graphics g = Graphics.FromImage(image))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.Clear(Color.White);
            for (int t = 0; t < podCount; t++)
            {
                g.FillRectangle(shadeBrush, -1, dh, width + 20 + 2, 25);
                g.DrawString(titles[t], font, textBrush, new PointF(5, dh));
                dh += 30;
                g.DrawImageUnscaled(images[t], 10, dh);
                dh += images[t].Height + 5;
            }

            string? btStr = "Wolfram Alpha Non-commercial API / AimuBot";
            g.FillRectangle(shadeBrush, 0, height + 40 - 20, width + 20, 20);
            var sz = g.MeasureString(btStr, font);

            textBrush.Color = Color.DarkRed;
            g.DrawString(btStr, font, textBrush,
                width + 30 - sz.Width - 10, height + 40 - 20, new StringFormat(StringFormatFlags.NoWrap));

            BotUtil.SaveImageToJpg(image, @"WolframAlpha\result.jpg", 95);
        }

        image.Dispose();
        images.ForEach(i => i.Dispose());
        images.Clear();

        return new MessageBuilder(ImageChain.Create("WolframAlpha/result.jpg", ImageChainType.LocalFile)).Build();
    }

    public async Task<string> GetString(string url)
    {
        string? html = "";
        ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((obj, cert, chain, errs) => true);
        using HttpClient hc = new();
        html = await hc.GetStringAsync(url);
        return html;
    }

    public async Task<byte[]> GetByteArray(string url)
    {
        ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((obj, cert, chain, errs) => true);
        using HttpClient hc = new();
        return await hc.GetByteArrayAsync(url);
    }

    public async Task DownloadFileFrom(string url, string relativePath)
    {
        byte[]? buff = await GetByteArray(url);
        await File.WriteAllBytesAsync(BotUtil.CombinePath(relativePath), buff);
    }
}
