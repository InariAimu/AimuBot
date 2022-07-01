using System.Drawing;
using System.Drawing.Drawing2D;

using AimuBot.Core.Config;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;
using AimuBot.Modules.Arcaea.AuaJson;
using AimuBot.Modules.Arcaea.SlstJson;

using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea : ModuleBase
{
    [Command("ac test",
        Name = "test",
        Tip = "/ac test",
        Level = RbacLevel.Super,
        Matching = Matching.Full,
        SendType = SendType.Send)]
    public MessageChain OnArcTest(BotMessage msg)
    {
        var songId = "ifi";
        Task.Run(async () =>
        {
            await GetSongDist(songId, 2);
            await msg.Bot.SendGroupMessageImage(msg.SubjectId, BotUtil.CombinePath($"Arcaea/songstat/{songId}.jpg"));
        });
        return "";
    }

    private async Task GetSongDist(string songId, int difficulty)
    {
        var startPtt = 988;
        var endPtt = 1288;
        var YSegments = 50;
        var dy = (endPtt - startPtt) / YSegments;
        var XSegments = 100;
        var dx = 1;

        Image im = new Bitmap(1000, 500);
        using Graphics g = Graphics.FromImage(im);
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        g.Clear(Color.White);


        Pen p = new Pen(Color.Aqua);
        p.Width = 1;
        var ft = new Font("Exo", 12, FontStyle.Regular);
        SolidBrush sb = new SolidBrush(Color.Black);

        var drawScoreLine = (int rating) =>
        {
            var x = (rating - 900) * 10;
            g.DrawLine(p, x, 0, x, 500);
            g.DrawString($"{rating}", ft, sb, x - 72 + 35, 500 - 20);
        };

        drawScoreLine(999);
        drawScoreLine(995);
        drawScoreLine(990);
        drawScoreLine(980);
        drawScoreLine(950);
        drawScoreLine(920);

        var drawPttLine = (int ptt) =>
        {
            var y = 500 - (ptt - startPtt) * 500 / (endPtt - startPtt);
            g.DrawLine(p, 0, y, 1000, y);
            g.DrawString($"{(float)ptt / 100:F2}", ft, sb, 2, y + 2);
        };

        p.Color = Color.Red;
        drawPttLine(1275);
        drawPttLine(1250);
        drawPttLine(1200);
        drawPttLine(1100);
        drawPttLine(1000);

        SolidBrush b = new SolidBrush(Color.Green);

        for (var i = 0; i < YSegments; i++)
        {
            var pr = await GetPlayData(songId, difficulty, startPtt + i * dy, startPtt + (i + 1) * dy);

            List<ScoreDistItem> line = new();
            var maxCount = 0;

            for (var j = 0; j < XSegments; j++)
            {
                var item = pr.Content.Find(x => x.Fscore == j + 900);
                if (item is null)
                    continue;

                maxCount = Math.Max((int)item.Count, maxCount);
                line.Add(item);
            }

            for (var j = 0; j < XSegments; j++)
            {
                var item = line.Find(x => x.Fscore == j + 900);
                if (item is null)
                    continue;

                Color cellColor = Color.FromArgb(240 * item.Count / maxCount + 15, Color.Green);
                b.Color = cellColor;
                g.FillRectangle(b, j * 10, 500 - 10 - i * 10, 10, 10);
            }
        }

        var f = new Font("GeosansLight", 32);
        b.Color = Color.Black;
        g.DrawString($"{songId} {difficulty}", f, b, 100, 40 - 22);

        im.SaveToJpg(BotUtil.CombinePath($"Arcaea/songstat/{songId}.jpg"), 100);
    }

    private string GetAllSongInfoFromAua()
    {
        Parallel.ForEach(_songInfoRaw.Songs.SongList, async song =>
        {
            var json = await GetFromBotArcApi($"song/info?songid={song.Id}");
            var r = JsonConvert.DeserializeObject<Response>(json);
            if (r is not { Content: { } }) return;
            for (var i = 0; i < r.Content.Difficulties.Count; i++)
            {
                var diff = r.Content.Difficulties[i];
                SongExtra songExtra = new()
                {
                    SongId = song.Id,
                    Difficulty = i,
                    Notes = diff.Note,
                    Rating = diff.Rating
                };
                _db.SaveObject(songExtra);
            }
        });
        return $"Arctest: {Enumerable.Sum<ArcaeaSongRaw>(_songInfoRaw.Songs.SongList, x => x.Difficulties.Count)}";
    }
    
    public void GetAllSongAliasFromBotArcApi()
    {
        foreach (var s in _songInfoRaw.Songs.SongList)
        {
            if (_arcaeaNameAlias.IsNameExist(s.Id))
                continue;

            var sr = GetFromBotArcApi($"song/alias?songid={s.Id}");
            sr.Wait();

            var json = sr.Result;

            Console.Write((string?)s.Id);

            var r = JsonConvert.DeserializeObject<Alias>(json);

            if (r is { Content: { } })
            {
                Console.Write(" " + r.Content.Count);
                r.Content.ForEach(x => _arcaeaNameAlias.SaveNameAlias(s.Id, x));
            }

            Console.WriteLine();

            Thread.Sleep(500);
        }
    }
}