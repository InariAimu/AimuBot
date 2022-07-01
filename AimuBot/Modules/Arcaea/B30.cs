using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;
using AimuBot.Modules.Arcaea.AuaJson;

using LunaUI.Layouts;

#pragma warning disable CA1416
#pragma warning disable CS8602

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea : ModuleBase
{
    private static readonly Dictionary<int, string> B30Styles = new()
    {
        { 0, "列表样式" },
        { 1, "图标样式" },
        { 2, "列表样式" }
    };

    [Command("ac b30 set v",
        Name = "设置b30样式",
        Description = "设置b30样式（推荐v2)",
        Tip = "/ac b30 set <style>",
        Example = "/ac b30 set v2",
        Category = "Arcaea",
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public MessageChain OnSetB30CardType(BotMessage msg)
    {
        if (msg.Content.IsNullOrEmpty())
            return "";

        var t = Convert.ToInt32(msg.Content);
        if (t < 0 || t >= B30Styles.Count)
            return "请使用 /ac b30 set <style_id> 设置 b30 卡片样式。\n" +
                   string.Join("\n", B30Styles.Select((k, v) => v));

        _db.SaveInt(msg.SenderId, "b30_type", t);
        return $"recent 样式已设置为 v{t}：{B30Styles[t]}";
    }

    [Command("ac b30",
        Name = "查询b30",
        Description = "查询b30",
        Tip = "/ac b30",
        Example = "/ac b30",
        Category = "Arcaea",
        CooldownType = CooldownType.User,
        CooldownSecond = 30,
        Matching = Matching.Full,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnB30(BotMessage msg)
    {
        var bindInfo = GetArcId(msg.SenderId);

        if (bindInfo == null)
            return "未绑定或id错误\n请使用/ac bind [arcaea数字id] 进行绑定";

        var arcIdOrName = bindInfo.BindType == 1 ? bindInfo.Name : bindInfo.ArcId;

        var response = await GetB30ResponseFromAua(arcIdOrName);

        bindInfo.ArcId = response.Content.AccountInfo.Code;
        bindInfo.Name = response.Content.AccountInfo.Name;
        _db.SaveObject(bindInfo);

        var accountInfo = response.Content.AccountInfo;

        foreach (var playRecord in response.Content.Best30List) UpdatePlayerScoreRecord(accountInfo, playRecord);

        double maxR10 = 0;
        for (var i = 0; i < response.Content.Best30List.Count && i < 10; i++) maxR10 += response.Content.Best30List[i].Rating;
        var maxPtt = (response.Content.Best30Avg * 30 + maxR10) / 40;

        PttHistoryDesc pttHistoryDesc = new()
        {
            ArcId = accountInfo.Code,
            Ptt = accountInfo.RealRating,
            B30 = response.Content.Best30Avg,
            R10 = response.Content.Recent10Avg,
            MaxB30 = maxPtt,
            Time = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalMilliseconds,
            Type = 0
        };
        _db.SaveObject(pttHistoryDesc);

        try
        {
            var succ = GetB30ImageFile(response, BotUtil.CombinePath("Arcaea/tmp_b30.jpg"), bindInfo.B30Type);
            if (succ) return new MessageBuilder(ImageChain.Create("Arcaea/tmp_b30.jpg")).Build();
        }
        catch (Exception ex)
        {
            return $"error: {ex.Message}";
        }

        return "";
    }

    private bool GetB30ImageFile(Response r, string path, int version, int overflow = 0)
    {
        var im = version switch
        {
            0 => GetB30_v2(r, overflow > 0),
            1 => GetB30_v1_Icon(r),
            2 => GetB30_v2(r, overflow > 0),
            _ => null
        };

        if (im == null) return false;

        im.SaveToJpg(BotUtil.CombinePath(path), 95);
        return true;
    }

    private Image? GetB30_v2(Response r, bool overflow = false)
    {
        var content = r.Content;

        if (content?.AccountInfo == null)
            return null;

        var cardCount = overflow ? 39 : 30;
        LunaUI.LunaUI ui = new(Core.Bot.Config.ResourcePath,
            overflow ? "Arcaea/ui/b40_v2.json" : "Arcaea/ui/b30_v2.json");

        ui.GetNodeByPath<LuiText>("title/char_bg/name").Text = content.AccountInfo.Name;

        if (content.AccountInfo.Rating >= 0)
        {
            ui.GetNodeByPath<LuiText>("title/char_bg/ptt_bg/ptt_l").Text = content.AccountInfo.Rating / 100 + ".";
            ui.GetNodeByPath<LuiText>("title/char_bg/ptt_bg/ptt_r").Text =
                (content.AccountInfo.Rating % 100).ToString("D2");
        }
        else
        {
            ui.GetNodeByPath<LuiText>("title/char_bg/ptt_bg/ptt_l").Text = "--";
            ui.GetNodeByPath<LuiText>("title/char_bg/ptt_bg/ptt_l").Position = new Point(-18, -2);
            ui.GetNodeByPath<LuiText>("title/char_bg/ptt_bg/ptt_r").PlaceHolder = "";
        }

        var ratingBgNo = content.AccountInfo.Rating switch
        {
            < 0     => "off",
            >= 1250 => "6",
            >= 1200 => "5",
            >= 1100 => "4",
            >= 1000 => "3",
            >= 800  => "2",
            >= 500  => "1",
            _       => "0"
        };
        ui.GetNodeByPath<LuiImage>("title/char_bg/ptt_bg").ImagePath = $"Arcaea/assets/img/rating_{ratingBgNo}.png";

        var sn = content.AccountInfo.IsCharUncapped ? "u" : "";
        ui.GetNodeByPath<LuiImage>("title/char_bg/char").ImagePath =
            $"Arcaea/assets/char/{content.AccountInfo.Character}{sn}_icon.png";

        ui.GetNodeByPath<LuiText>("title/b30").Text = $"Best30 {content.Best30Avg:F3}";
        double maxR10 = 0;
        for (var i = 0; i < content.Best30List.Count && i < 10; i++) maxR10 += content.Best30List[i].Rating;
        var maxPtt = (content.Best30Avg * 30 + maxR10) / 40;
        ui.GetNodeByPath<LuiText>("title/b30max").Text = $"MaxPtt {maxPtt:F3}";
        
        ui.GetNodeByPath<LuiText>("title/r10").Text = content.AccountInfo.Rating >= 0 ? $"Recent10 {content.Recent10Avg:F3}" : " ";

        var b30Table = ui.GetNodeByPath<LuiCloneTableLayout>("b30_table");
        b30Table.CloneLayouts(cardCount, "block_");

        double minRating = 0;
        
        for (var i = 0; i < cardCount; i++)
        {
            PlayRecord playInfo;
            if (i < content.Best30List.Count)
                playInfo = content.Best30List[i];
            else if (i - content.Best30List.Count < content.Best30Overflow.Count)
                playInfo = content.Best30Overflow[i - content.Best30List.Count];
            else
                break;

            if (i == content.Best30List.Count - 1)
                minRating = playInfo.Rating;
            
            var songInfoRaw = _songInfoRaw[playInfo.SongId];

            ui.GetNodeByPath<LuiImage>($"b30_table/block_{i}/cover").ImagePath =
                songInfoRaw.GetCover(playInfo.Difficulty);

            var songInfoRawDiff = songInfoRaw.Difficulties[playInfo.Difficulty];
            var cardName = $"b30_table/block_{i}/name";

            var (songNameFont, songNameText) = songInfoRaw.GetSongFontAndName(playInfo.Difficulty);
            ui.GetNodeByPath<LuiText>(cardName).Font = songNameFont;
            ui.GetNodeByPath<LuiText>(cardName).Text = songNameText;

            ui.GetNodeByPath<LuiText>($"b30_table/block_{i}/score").Text =
                playInfo.Score.ToString("D8").Insert(5, "\'").Insert(2, "\'");

            ui.GetNodeByPath<LuiText>($"b30_table/block_{i}/title/idx").Text = $"#{i + 1}";
            
            if (playInfo.Score >= 10000000)
                ui.GetNodeByPath<LuiText>($"b30_table/block_{i}/title/rating").Color = Color.DarkCyan;

            var rating = GetRating(playInfo.SongId, playInfo.Difficulty);
            var ratingStr = "";
            if (rating > 0)
            {
                ratingStr = rating.ToString("F1");

                if (i >= content.Best30List.Count)
                {
                    if (rating + 2 <= minRating)
                        ui.GetNodeByPath<LuiText>($"b30_table/block_{i}/title/rating").Color = Color.Red;
                }
            }
            else
            {
                ratingStr = songInfoRawDiff.Rating.ToString();
                if (songInfoRawDiff.RatingPlus)
                    ratingStr += "+";
            }

            ui.GetNodeByPath<LuiText>($"b30_table/block_{i}/title/rating").Text =
                $"{ratingStr} > {playInfo.Rating:F3}";

            ui.GetNodeByPath<LuiText>($"b30_table/block_{i}/perf/pure").Text = playInfo.PerfectCount.ToString();
            ui.GetNodeByPath<LuiText>($"b30_table/block_{i}/perf/good").Text =
                (playInfo.PerfectCount - playInfo.ShinyPerfectCount).ToString();
            ui.GetNodeByPath<LuiText>($"b30_table/block_{i}/perf/far").Text = playInfo.NearCount.ToString();
            ui.GetNodeByPath<LuiText>($"b30_table/block_{i}/perf/lost").Text = playInfo.MissCount.ToString();

            var songPlayedTime = DateTimeOffset.FromUnixTimeMilliseconds(playInfo.TimePlayed).LocalDateTime;
            var ts = DateTime.Now - songPlayedTime;
            var timeToNow = ts.TotalMinutes switch
            {
                > 1440 => $"{(int)ts.TotalMinutes / 1440}d",
                > 60   => $"{(int)ts.TotalMinutes / 60}h",
                > 1    => $"{(int)ts.TotalMinutes}m",
                _      => "now"
            };
            ui.GetNodeByPath<LuiText>($"b30_table/block_{i}/date").Text = timeToNow;

            if (ts.TotalMinutes < 1440 * 3)
            {
                ui.GetNodeByPath<LuiRect>($"b30_table/block_{i}/cover/new").Visible = true;
                ui.GetNodeByPath<LuiRect>($"b30_table/block_{i}/cover/new").Color = ts.TotalMinutes switch
                {
                    < 180  => Color.Red,
                    < 1440 => Color.LightCoral,
                    _      => Color.Orange
                };
            }
            else
            {
                ui.GetNodeByPath<LuiRect>($"b30_table/block_{i}/cover/new").Visible = false;
            }

            var (_, diffColor) = playInfo.Difficulty switch
            {
                0 => ("Past", Color.Blue),
                1 => ("Present", Color.ForestGreen),
                2 => ("Future", Color.FromArgb(196, 22, 174)),
                3 => ("Beyond", Color.FromArgb(192, 0, 0)),
                _ => ("Unknown", Color.White)
            };

            var diffcultyShadeColor = Color.FromArgb(64, diffColor);
            ui.GetNodeByPath<LuiColorLayer>($"b30_table/block_{i}/title").Color = diffcultyShadeColor;
            ui.GetNodeByPath<LuiColorLayer>($"b30_table/block_{i}/shade_1").Color = diffcultyShadeColor;
            ui.GetNodeByPath<LuiColorLayer>($"b30_table/block_{i}/shade_2").Color = diffcultyShadeColor;
        }

        Image im = new Bitmap(ui.Root.Option.CanvasSize.Width * 80 / 100, ui.Root.Option.CanvasSize.Height * 80 / 100);
        var origin = ui.Render();

        using var g = Graphics.FromImage(im);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.DrawImage(origin,
            Rectangle.FromLTRB(0, 0, im.Width, im.Height),
            Rectangle.FromLTRB(0, 0, origin.Width, origin.Height),
            GraphicsUnit.Pixel);

        return im;
    }

    private Image GetB30_v1_Icon(Response r)
    {
        Image img = new Bitmap(1284, 2778);

        using var g = Graphics.FromImage(img);

        g.Clear(Color.White);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;
        g.TextContrast = 8;
        var sb = new SolidBrush(Color.White);

        var bg = Image.FromFile(Core.Bot.Config.ResourcePath.CombinePath("Arcaea/assets/startup/bg.jpg"));
        var rateX = (double)bg.Width / 1284;
        var rateY = (double)bg.Height / 2778;
        if (rateX < rateY)
        {
            var w = (int)(1284 * rateY);
            var x = bg.Width - w;
            g.DrawImage(bg, Rectangle.FromLTRB(0, 0, 1284, 2778), Rectangle.FromLTRB(x + w / 2, w, 0, bg.Height),
                GraphicsUnit.Pixel);
        }
        else
        {
            var y = 2778 * rateX;
            g.DrawImage(bg, Rectangle.FromLTRB(0, 0, 1284, 2778), Rectangle.FromLTRB(0, 0, bg.Width, bg.Height),
                GraphicsUnit.Pixel);
        }

        sb.Color = Color.FromArgb(128, 0, 0, 0);
        g.FillRectangle(sb, 0, 0, 1284, 2778);

        for (var i = -2; i < r.Content.Best30List.Count; i++)
        {
            PlayRecord? single = null;
            if (i >= 0)
                single = r.Content.Best30List[i];
            var sim = GetB30_v1_Icon_Single(i, r.Content, single);
            var cardIndex = i + 2;
            var x = cardIndex % 4;
            var y = cardIndex / 4;
            g.DrawImage(sim, (1284 - 290 * 4) / 2 + x * 290, y * 321 + 120);
        }

        var f = new FontFamily("GeosansLight");
        var ft = new Font(f, 40f, FontStyle.Regular);
        var sf = new StringFormat();
        sf.Alignment = StringAlignment.Near;

        sb.Color = Color.White;
        var dt = DateTime.Now;
        g.DrawString($"{dt.Hour:D2}:{dt.Minute:D2}", ft, sb, Rectangle.FromLTRB(109, 53, 109 + 400, 53 + 60), sf);
        g.DrawString("Aimubot", ft, sb, Rectangle.FromLTRB(1015, 53, 1039 + 400, 53 + 60), sf);

        return img;
    }

    public Image GetB30_v1_Icon_Single(int id, Content r, PlayRecord pr)
    {
        Image img = new Bitmap(300, 320);
        using var g = Graphics.FromImage(img);

        //g.Clear(Color.Black);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = TextRenderingHint.AntiAlias;

        const int x = 300 / 2 - 220 / 2;
        var gp = GetRoundedRect(Rectangle.FromLTRB(x, x, x + 220, x + 220), 40);

        var sb = new SolidBrush(Color.White);
        var f = new FontFamily("Exo");
        var ft = new Font(f, 24f, FontStyle.Bold);
        var sf = new StringFormat();
        sf.Alignment = StringAlignment.Center;

        switch (id)
        {
            case -2:
            {
                sb.Color = Color.FromArgb(64, 255, 255, 255);
                g.FillPath(sb, gp);

                var _f = Core.Bot.Config.ResourcePath.CombinePath(
                    $"Arcaea/assets/char/{r.AccountInfo.Character}_icon.png");
                var fi = new FileInfo(_f);
                if (fi.Exists)
                {
                    var im = Image.FromFile(_f);
                    var _s = 190;
                    var xc = 300 / 2 - _s / 2;
                    g.DrawImage(im, Rectangle.FromLTRB(xc, xc, xc + _s, xc + _s),
                        Rectangle.FromLTRB(0, 0, im.Width, im.Height), GraphicsUnit.Pixel);
                }

                sb.Color = Color.White;
                g.DrawString(r.AccountInfo.Name, ft, sb, Rectangle.FromLTRB(0, x + 220 + 5, 300, 330), sf);
                break;
            }
            case -1:
            {
                sb.Color = Color.FromArgb(64, 255, 255, 255);
                g.FillPath(sb, gp);

                var rt = r.AccountInfo.Rating switch
                {
                    < 0    => -1,
                    < 350  => 0,
                    < 700  => 1,
                    < 1000 => 2,
                    < 1100 => 3,
                    < 1200 => 4,
                    < 1250 => 5,
                    _      => 6
                };
                var rs = $"rating_{rt}.png";
                if (rt < 0)
                    rs = "rating_off.png";

                var im = Image.FromFile(Core.Bot.Config.ResourcePath.CombinePath($"Arcaea/assets/img/{rs}"));
                var xc = 300 / 2 - 240 / 2;
                g.DrawImage(im, Rectangle.FromLTRB(xc, xc, xc + 240, xc + 240),
                    Rectangle.FromLTRB(0, 0, im.Width, im.Height), GraphicsUnit.Pixel);

                var fts = new Font(f, 46f, FontStyle.Bold);
                sf.Alignment = StringAlignment.Center;
                var _s = $"{(double)r.AccountInfo.Rating / 100:F2}";
                sb.Color = Color.FromArgb(200, 0, 0, 0);
                var _w = 3;
                g.DrawString(_s, fts, sb, Rectangle.FromLTRB(_w, xc + 70, 300, xc + 70 + 100), sf);
                g.DrawString(_s, fts, sb, Rectangle.FromLTRB(-_w, xc + 70, 300, xc + 70 + 100), sf);
                g.DrawString(_s, fts, sb, Rectangle.FromLTRB(0, xc + 70 - _w, 300, xc + 70 + 100), sf);
                g.DrawString(_s, fts, sb, Rectangle.FromLTRB(0, xc + 70 + _w, 300, xc + 70 + 100), sf);
                sb.Color = Color.White;
                g.DrawString(_s, fts, sb, Rectangle.FromLTRB(0, xc + 70, 300, xc + 70 + 100), sf);

                sb.Color = Color.White;
                g.DrawString($"{r.Best30Avg:F3} / {r.Recent10Avg:F3}", ft, sb,
                    Rectangle.FromLTRB(0, x + 220 + 5, 300, 330), sf);
                break;
            }
            case >= 0:
            {
                var songRaw = _songInfoRaw.Songs.SongList.Find(s => s.Id == pr.SongId);

                var good = pr.PerfectCount - pr.ShinyPerfectCount;

                if (songRaw != null)
                {
                    var s = BotUtil.CombinePath(songRaw.GetCover(pr.Difficulty));
                    var im = Image.FromFile(s);
                    Image songCover = new Bitmap(300, 300);
                    using (var g2 = Graphics.FromImage(songCover))
                    {
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        g2.DrawImage(im, Rectangle.FromLTRB(x, x, x + 220, x + 220),
                            Rectangle.FromLTRB(0, 0, im.Width, im.Height), GraphicsUnit.Pixel);

                        var lgb = new LinearGradientBrush(
                            new Point(0, x + 220 - 60 - 1),
                            new Point(0, x + 220),
                            Color.FromArgb(0, 0, 0, 0),
                            Color.FromArgb(200, 0, 0, 0));

                        g2.FillRectangle(lgb, Rectangle.FromLTRB(0, x + 220 - 60, 300, 300));
                    }

                    var tb = new TextureBrush(songCover);
                    g.FillPath(tb, gp);
                }
                else
                {
                    sb.Color = Color.FromArgb(64, 255, 255, 255);
                    g.FillPath(sb, gp);

                    sb.Color = Color.White;
                    g.DrawString(pr.SongId, ft, sb, Rectangle.FromLTRB(x, x, x + 220, x + 220), sf);
                }

                var scoreText = pr.Score.ToString("D8").Insert(5, "\'").Insert(2, "\'");
                if (pr.Score > 10000000 && good == 0)
                {
                    sb.Color = Color.FromArgb(200, 0, 255, 186);
                    g.DrawString(scoreText, ft, sb, Rectangle.FromLTRB(2, x + 220 + 5 + 2, 300, 330), sf);
                }

                sb.Color = Color.White;
                g.DrawString(scoreText, ft, sb, Rectangle.FromLTRB(0, x + 220 + 5, 300, 330), sf);

                var gp2 = GetRoundedRect(Rectangle.FromLTRB(x + 220 + 20 - 100, x - 26, x + 220 + 20, x + 26), 26);
                sb.Color = pr.Difficulty switch
                {
                    0 => Color.Blue,
                    1 => Color.ForestGreen,
                    2 => Color.FromArgb(196, 22, 174),
                    3 => Color.Red,
                    _ => Color.Gray
                };
                g.FillPath(sb, gp2);

                var fts = new Font(f, 24f, FontStyle.Regular);
                sb.Color = Color.White;
                g.DrawString($"{pr.Rating:F2}", fts, sb,
                    Rectangle.FromLTRB(x + 220 + 20 - 100, x - 26 + 6, x + 220 + 20, x + 26), sf);

                var ftc = new Font(f, 12f, FontStyle.Bold);
                sb.Color = Color.White;
                sf.Alignment = StringAlignment.Center;

                var songResultText = "";
                switch (pr.Score)
                {
                    case > 10000000 when good == 0:
                        songResultText = "";
                        break;
                    case > 10000000:
                        songResultText = $"P{pr.PerfectCount} -{good}";
                        break;
                    default:
                    {
                        var rating = GetRating(pr.SongId, pr.Difficulty);
                        if (rating > 0)
                            songResultText = $"{rating:F1}  " +
                                             $"P{pr.PerfectCount} -{good}  " +
                                             $"F{pr.NearCount} L{pr.MissCount}";
                        else
                            songResultText =
                                $"P{pr.PerfectCount} -{good}  " +
                                $"F{pr.NearCount} L{pr.MissCount}";
                        break;
                    }
                }

                g.DrawString(songResultText, ftc, sb, Rectangle.FromLTRB(x, x + 220 - 25, x + 220, x + 220), sf);
                break;
            }
        }

        return img;
    }

    private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
    {
        radius *= 2;

        GraphicsPath gp = new();
        gp.AddLine(rect.X + radius, rect.Y, rect.Right - radius, rect.Y);
        gp.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);

        gp.AddLine(rect.Right, rect.Y + radius, rect.Right, rect.Y - radius);
        gp.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);

        gp.AddLine(rect.X + radius, rect.Bottom, rect.Right - radius, rect.Bottom);
        gp.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);

        gp.AddLine(rect.X, rect.Y + radius, rect.X, rect.Bottom - radius);
        gp.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
        return gp;
    }
}