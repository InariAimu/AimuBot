using System.Drawing;
using System.Drawing.Drawing2D;

using AimuBot.Core.Config;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;
using AimuBot.Modules.Arcaea.AuaJson;

using LunaUI.Layouts;

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea : ModuleBase
{
    private static readonly Dictionary<int, string> RecentStyles = new()
    {
        { 0, "Arcaea" },
        { 1, "Phigros" }
    };

    [Command("ac set v",
        Name = "设置recent样式",
        Description = "设置recent样式：\nv0：Arcaea\nv1：Phigros\n...",
        Tip = "/ac set <style_id>",
        Example = "/ac set v0",
        Category = "Arcaea",
        Matching = Matching.StartsWith,
        Level = RbacLevel.Normal,
        SendType = SendType.Reply)]
    public MessageChain OnSetRecentCardType(BotMessage msg)
    {
        if (msg.Content.IsNullOrEmpty())
            return "";

        var t = Convert.ToInt32(msg.Content);
        if (t < 0 || t >= RecentStyles.Count)
            return "请使用 /ac set <style_id> 设置 recent 卡片样式。\n" +
                   string.Join("\n", RecentStyles.Select(kvp => $"v{kvp.Key}: {kvp.Value}"));

        _db.SaveInt(msg.SenderId, "recent_type", t);
        return $"recent 样式已设置为 v{t}：{RecentStyles[t]}";
    }

    [Command("ac",
        Name = "Recent",
        Description = "获取最近一次游玩成绩",
        Tip = "/ac",
        Example = "/ac",
        Category = "Arcaea",
        CooldownType = CooldownType.User,
        CooldownSecond = 30,
        Matching = Matching.Full,
        Level = RbacLevel.Normal,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnRecent(BotMessage msg)
    {
        LogMessage($"[ArcRecent] {msg.SenderId}");

        var bindInfo = GetArcId(msg.SenderId);

        if (bindInfo == null)
            return "未绑定或id错误\n请使用/ac bind [arcaea数字id] 进行绑定";

        var arcIdOrName = bindInfo.BindType == 1 ? bindInfo.Name : bindInfo.ArcId;

        var response = await GetUserRecent(arcIdOrName);
        if (response is null)
            return "查询出错。";

        if (response.Status < 0)
            return $"查询出错 {response.Status}: {response.Message}";

        bindInfo.ArcId = response.Content.AccountInfo.Code;
        bindInfo.Name = response.Content.AccountInfo.Name;
        _db.SaveObject(bindInfo);

        UpdatePlayerScoreRecord(response.Content.AccountInfo, response.Content.RecentScore[0]);
        UpdatePttHistory(bindInfo.ArcId, response.Content.AccountInfo.RealRating,
            response.Content.RecentScore[0].TimePlayed);

        var im = GetRecentImage(response, bindInfo.ArcId, bindInfo.RecentType);
        im?.SaveToJpg(BotUtil.CombinePath($"Arcaea/recents/{arcIdOrName}.jpg"), 90);

        return im is not null
            ? new MessageBuilder(ImageChain.Create($"Arcaea/recents/{arcIdOrName}.jpg")).Build()
            : "图片生成出错。";
    }

    [Command("ac usr",
        Name = "指定玩家Recent",
        Description = "获取指定玩家最近一次游玩成绩",
        Tip = "/ac usr <arc_id>",
        Example = "/ac usr ToasterKoishi\n/ac usr 582325489",
        Category = "Arcaea",
        CooldownType = CooldownType.User,
        CooldownSecond = 10,
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public async Task<MessageChain> OnRecentUsr(BotMessage msg)
    {
        var content = msg.Content;

        var response = await GetUserRecent(msg.Content);
        if (response.Status < 0)
            return $"查询出错 {response.Status}: {response.Message}";

        var im = GetRecentImage(response, response.Content.AccountInfo.Code, 0);
        im.SaveToJpg(BotUtil.CombinePath($"Arcaea/recents/{content}.jpg"), BotUtil.Random.Next(90, 100));

        return new MessageBuilder(ImageChain.Create($"Arcaea/recents/{content}.jpg")).Build();
    }

    internal Image? GetRecentImage(Response recent, string arc_id, int recent_type) => recent_type switch
    {
        0 => GetRecentImage_Arcaea(recent, arc_id),
        1 => GetRecentImage_Phigros(recent, arc_id),
        _ => null
    };

    internal Image? GetRecentImage_Phigros(Response recent, string arc_id)
    {
        var content = recent.Content;
        if (content == null)
            return null;

        if (content.AccountInfo == null)
            return null;

        PlayRecord? play_info;

        if (content.RecentScore != null)
            play_info = content.RecentScore[0];
        else
            play_info = content.Record;

        LunaUI.LunaUI ui = new(Core.Bot.Config.ResourcePath, "Arcaea/ui/arc_recent_v1.json");

        ui.GetNodeByPath<LuiText>("name").Text = content.AccountInfo.Name;

        if (content.AccountInfo.Rating >= 0)
            ui.GetNodeByPath<LuiText>("ptt").Text = ((float)content.AccountInfo.Rating / 100).ToString("F2");
        else
            ui.GetNodeByPath<LuiText>("ptt").Text = "--";

        var lastPttInfos = _db.GetObjects<PttHistoryDesc>(
            "arc_id = $arc_id order by [time] desc limit 0,2",
            new Dictionary<string, object> { { "$arc_id", arc_id } }
        );

        string ptt_diff;
        if (lastPttInfos != null && lastPttInfos.Length > 0)
        {
            double ptt_diff_d = 0;
            if (lastPttInfos[0].Time == play_info.TimePlayed)
            {
                if (lastPttInfos.Length > 1)
                    ptt_diff_d = content.AccountInfo.RealRating - lastPttInfos[1].Ptt;
            }
            else
            {
                ptt_diff_d = content.AccountInfo.RealRating - lastPttInfos[0].Ptt;
            }

            ptt_diff = $"{ptt_diff_d:F2}";
            if (ptt_diff_d > 0)
            {
                ptt_diff = "+" + ptt_diff;
                ui.GetNodeByPath<LuiText>("ptt_diff").Text = ptt_diff;
                ui.GetNodeByPath<LuiText>("ptt_diff").Color = Color.FromArgb(255, 172, 255, 255);
            }
            else if (ptt_diff_d == 0)
            {
                ui.GetNodeByPath<LuiText>("ptt_diff").Visible = false;
            }
            else if (ptt_diff_d < 0)
            {
                ui.GetNodeByPath<LuiText>("ptt_diff").Text = ptt_diff;
                ui.GetNodeByPath<LuiText>("ptt_diff").Color = Color.FromArgb(255, 255, 172, 172);
            }
        }

        var sn = content.AccountInfo.IsCharUncapped ? "u" : "";
        ui.GetNodeByPath<LuiImage>("char").ImagePath =
            $"Arcaea/assets/char/{content.AccountInfo.Character}{sn}_icon.png";

        var song_info_raw = _songInfoRaw[play_info.SongId];
        ui.GetNodeByPath<LuiImage>("cover").ImagePath = song_info_raw.GetCover(play_info.Difficulty);

        var grade_text = GetGradeText(play_info.Score, true);

        ui.GetNodeByPath<LuiImage>("right/clear_type").ImagePath = $"Arcaea/ArcRecent_Phi/{grade_text}.png";

        ui.GetNodeByPath<LuiText>("right/score").Text = play_info.Score.ToString("D8");

        //get high-score
        var high_scores = _db.GetObjects<ScoreDesc>(
            "arc_id=$arc_id and song_id=$song_id and diff=$diff order by [score] desc limit 0,2",
            new Dictionary<string, object>
                { { "$arc_id", arc_id }, { "$song_id", play_info.SongId }, { "$diff", play_info.Difficulty } }
        );

        if (high_scores.Length > 0)
        {
            var hs = 0;
            if (high_scores[0].time == play_info.TimePlayed)
            {
                if (high_scores.Length > 1)
                    hs = high_scores[1].Score;
            }
            else
            {
                hs = high_scores[0].Score;
            }

            var best_text = "";

            var score_diff = play_info.Score - hs;

            best_text += score_diff > 0 ? "NEW BEST " : "BEST ";
            best_text += hs.ToString("D8");
            best_text += " ";
            best_text += (score_diff >= 0 ? "+" : "") + score_diff.ToString("D8");
            ui.GetNodeByPath<LuiText>("right/best").Text = best_text;
        }
        else
        {
            ui.GetNodeByPath<LuiText>("right/best").Text = " ";
        }

        ui.GetNodeByPath<LuiText>("right/pure").Text = play_info.PerfectCount.ToString();
        ui.GetNodeByPath<LuiText>("right/good").Text =
            (play_info.PerfectCount - play_info.ShinyPerfectCount).ToString();
        ui.GetNodeByPath<LuiText>("right/far").Text = play_info.NearCount.ToString();
        ui.GetNodeByPath<LuiText>("right/lost").Text = play_info.MissCount.ToString();

        var acc = (float)play_info.Score * 100 / 10000000;
        if ((int)play_info.ClearType == 3)
            acc = 100 + (float)play_info.ShinyPerfectCount / 100;

        ui.GetNodeByPath<LuiText>("right/acc").Text = acc.ToString("F2") + "%";

        var song_info_raw_diff = song_info_raw.Difficulties[play_info.Difficulty];
        if (song_info_raw_diff.TitleLocalized != null)
        {
            if (!StringExtension.IsNullOrEmpty(song_info_raw_diff.TitleLocalized.Ja))
            {
                ui.GetNodeByPath<LuiText>("song_name").Font = "Kazesawa Regular";
                ui.GetNodeByPath<LuiText>("song_name").Text = song_info_raw_diff.TitleLocalized.Ja;
            }
            else
            {
                ui.GetNodeByPath<LuiText>("song_name").Font = "Exo";
                ui.GetNodeByPath<LuiText>("song_name").Text = song_info_raw_diff.TitleLocalized.En;
            }
        }
        else
        {
            if (!StringExtension.IsNullOrEmpty(song_info_raw.TitleLocalized.Ja))
            {
                ui.GetNodeByPath<LuiText>("song_name").Font = "Kazesawa Regular";
                ui.GetNodeByPath<LuiText>("song_name").Text = song_info_raw.TitleLocalized.Ja;
            }
            else
            {
                ui.GetNodeByPath<LuiText>("song_name").Font = "Noto Sans CJK SC Regular";
                ui.GetNodeByPath<LuiText>("song_name").Text = song_info_raw.TitleLocalized.En;
            }
        }

        int bg_id;
        if (song_info_raw.Side == 0)
            (_, bg_id) = new List<int> { 0, 5, 1, 3 }.RandomWhenNotEmpty();
        else
            (_, bg_id) = new List<int> { 2, 3, 4, 6 }.RandomWhenNotEmpty();
        ui.GetNodeByPath<LuiImage>("bg").ImagePath = $"Arcaea/ArcRecent_Phi/{bg_id}_b.png";

        var rating_f = GetRating(play_info.SongId, play_info.Difficulty);

        var difft_str = song_info_raw_diff.Rating.ToString();
        if (song_info_raw_diff.RatingPlus)
            difft_str += "+";

        var rating_str = difft_str;
        if (rating_f > 0) rating_str = GetRating(play_info.SongId, play_info.Difficulty).ToString("F1");

        var diff_str = GetShortDifficultyText(play_info.Difficulty);

        ui.GetNodeByPath<LuiText>("diff").Text = diff_str + " Lv." + difft_str;

        ui.GetNodeByPath<LuiText>("right/rating").Text = rating_str + " > " + play_info.Rating.ToString("F3");

        ui.GetNodeByPath<LuiText>("datetime").Text =
            DateTimeOffset.FromUnixTimeMilliseconds(play_info.TimePlayed).LocalDateTime.ToString();

        Image im = new Bitmap(ui.Root.Option.CanvasSize.Width * 85 / 100, ui.Root.Option.CanvasSize.Height * 85 / 100);
        var origin = ui.Render();
        using (var g = Graphics.FromImage(im))
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(origin,
                Rectangle.FromLTRB(0, 0, im.Width, im.Height),
                Rectangle.FromLTRB(0, 0, origin.Width, origin.Height),
                GraphicsUnit.Pixel);
        }

        return im;
    }

    private Image? GetRecentImage_Arcaea(Response recent, string arc_id, int ranking = 0)
    {
        var content = recent.Content;

        if (content?.AccountInfo == null)
            return null;

        PlayRecord? playInfo = null;

        if (content.RecentScore != null)
            playInfo = content.RecentScore[0];
        else
            playInfo = content.Record;

        LunaUI.LunaUI ui = new(BotUtil.ResourcePath, "Arcaea/ui/arc_recent.json");

        if (ranking == 0)
        {
            ui.GetNodeByPath<LuiImage>("top_bar/user_info/rank_bg").Visible = false;
            ui.GetNodeByPath<LuiImage>("top_bar/user_info/rank_frame").Visible = false;
            ui.GetNodeByPath<LuiImage>("top_bar/user_info/pott_bg").Position = new Point(110, -45);
        }

        ui.GetNodeByPath<LuiText>("top_bar/user_info/player_name").Text = content.AccountInfo.Name;

        if (content.AccountInfo.Rating >= 0)
        {
            ui.GetNodeByPath<LuiText>("top_bar/user_info/ptt_bg/ptt_int").Text = content.AccountInfo.Rating / 100 + ".";
            ui.GetNodeByPath<LuiText>("top_bar/user_info/ptt_bg/ptt_tail").Text =
                (content.AccountInfo.Rating % 100).ToString("D2");
        }
        else
        {
            ui.GetNodeByPath<LuiText>("top_bar/user_info/ptt_bg/ptt_int").Text = "--";
            ui.GetNodeByPath<LuiText>("top_bar/user_info/ptt_bg/ptt_int").Position = new Point(-18, -2);
            ui.GetNodeByPath<LuiText>("top_bar/user_info/ptt_bg/ptt_tail").PlaceHolder = "";
        }

        var rating_bg_no = content.AccountInfo.Rating switch
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
        ui.GetNodeByPath<LuiImage>("top_bar/user_info/ptt_bg").ImagePath =
            $"Arcaea/assets/img/rating_{rating_bg_no}.png";

        var sn = content.AccountInfo.IsCharUncapped ? "u" : "";
        ui.GetNodeByPath<LuiImage>("top_bar/user_info/char_bg/char").ImagePath =
            $"Arcaea/assets/char/{content.AccountInfo.Character}{sn}_icon.png";

        ui.GetNodeByPath<LuiImage>("banner/char").ImagePath =
            $"Arcaea/assets/char/{content.AccountInfo.Character}{sn}.png";

        var lastPttInfos = _db.GetObjects<PttHistoryDesc>(
            "arc_id = $arc_id order by [time] desc limit 0,2",
            new Dictionary<string, object> { { "$arc_id", arc_id } }
        );
        string ptt_diff;
        if (lastPttInfos != null && lastPttInfos.Length > 0)
        {
            double ptt_diff_d = 0;
            if (lastPttInfos[0].Time == playInfo.TimePlayed)
            {
                if (lastPttInfos.Length > 1)
                    ptt_diff_d = content.AccountInfo.RealRating - lastPttInfos[1].Ptt;
            }
            else
            {
                ptt_diff_d = content.AccountInfo.RealRating - lastPttInfos[0].Ptt;
            }

            ptt_diff = $"{ptt_diff_d:F2}";
            switch (ptt_diff_d)
            {
                case > 0:
                    ptt_diff = "+" + ptt_diff;
                    ui.GetNodeByPath<LuiText>("top_bar/user_info/pott_bg/ptt_add").Text = ptt_diff;
                    ui.GetNodeByPath<LuiText>("top_bar/user_info/pott_bg/ptt_add").BorderColor =
                        Color.FromArgb(60, 128, 255, 255);
                    ui.GetNodeByPath<LuiImage>("top_bar/user_info/pott_bg").ImagePath =
                        "Arcaea/assets/layouts/results/rating_up.png";
                    break;
                case 0:
                    ui.GetNodeByPath<LuiText>("top_bar/user_info/pott_bg/ptt_add").PlaceHolder = "";
                    ui.GetNodeByPath<LuiImage>("top_bar/user_info/pott_bg/keep").Visible = true;
                    ui.GetNodeByPath<LuiImage>("top_bar/user_info/pott_bg").ImagePath =
                        "Arcaea/assets/layouts/results/rating_keep.png";
                    break;
                case < 0:
                    ui.GetNodeByPath<LuiText>("top_bar/user_info/pott_bg/ptt_add").Text = ptt_diff;
                    ui.GetNodeByPath<LuiText>("top_bar/user_info/pott_bg/ptt_add").BorderColor =
                        Color.FromArgb(60, 255, 128, 128);
                    ui.GetNodeByPath<LuiImage>("top_bar/user_info/pott_bg").ImagePath =
                        "Arcaea/assets/layouts/results/rating_down.png";
                    break;
            }

            if (ranking > 0)
            {
                ui.GetNodeByPath<LuiText>("top_bar/user_info/pott_bg/ptt_add").Visible = false;
                ui.GetNodeByPath<LuiImage>("top_bar/user_info/pott_bg/keep").Visible = true;
                ui.GetNodeByPath<LuiImage>("top_bar/user_info/pott_bg").ImagePath =
                    "Arcaea/assets/layouts/results/rating_keep.png";
                ui.GetNodeByPath<LuiText>("top_bar/user_info/rank_frame/Text").Text = ranking.ToString();
            }
        }
        else
        {
            ptt_diff = $"+{content.AccountInfo.RealRating:F2}";
            ui.GetNodeByPath<LuiText>("top_bar/user_info/pott_bg/ptt_add").Text = ptt_diff;
        }


        ui.GetNodeByPath<LuiText>("banner/score_board/score").Text =
            playInfo.Score.ToString("D8").Insert(5, "\'").Insert(2, "\'");
        if (playInfo.PerfectCount == playInfo.ShinyPerfectCount && playInfo.NearCount == 0 &&
            playInfo.MissCount == 0)
        {
            ui.GetNodeByPath<LuiText>("banner/score_board/score").ShadeColor = Color.FromArgb(150, 0, 192, 192);
        }
        else
        {
            ui.GetNodeByPath<LuiText>("banner/score_board/score").ShadeColor = Color.FromArgb(40, 40, 40);
            ui.GetNodeByPath<LuiText>("banner/score_board/score").ShadeDisplacement = 3;
        }

        var grade_text = GetGradeText(playInfo.Score);

        var (clear_img, clear_img_height) = (int)playInfo.ClearType switch
        {
            0 => ("fail", 55),
            2 => ("full", 75),
            3 => ("pure", 75),
            _ => ("normal", 55)
        };

        ui.GetNodeByPath<LuiImage>("banner/clear_type").Size = new Size(700, clear_img_height);
        ui.GetNodeByPath<LuiImage>("banner/clear_type").ImagePath = $"Arcaea/assets/img/clear_{clear_img}.png";

        ui.GetNodeByPath<LuiImage>("banner/score_board/grade").ImagePath = $"Arcaea/assets/img/grade_{grade_text}.png";

        //get high-score
        var high_scores = _db.GetObjects<ScoreDesc>(
            "arc_id=$arc_id and song_id=$song_id and diff=$diff order by [score] desc limit 0,2",
            new Dictionary<string, object>
                { { "$arc_id", arc_id }, { "$song_id", playInfo.SongId }, { "$diff", playInfo.Difficulty } }
        );

        if (high_scores.Length > 0)
        {
            var hs = 0;
            if (high_scores[0].time == playInfo.TimePlayed)
            {
                if (high_scores.Length > 1)
                    hs = high_scores[1].Score;
            }
            else
            {
                hs = high_scores[0].Score;
            }

            ui.GetNodeByPath<LuiText>("banner/score_board/hi_score").Text =
                hs.ToString("D8").Insert(5, "\'").Insert(2, "\'");

            var score_diff = playInfo.Score - hs;
            var score_diff_text =
                (score_diff >= 0 ? "+" : "") + score_diff.ToString("D8").Insert(5, "\'").Insert(2, "\'");
            ui.GetNodeByPath<LuiText>("banner/score_board/score_diff").Text = score_diff_text;

            if (score_diff <= 0)
                ui.GetNodeByPath<LuiImage>("banner/score_board").ImagePath =
                    "Arcaea/assets/layouts/results/res_scoresection.png";
        }
        else
        {
            ui.GetNodeByPath<LuiText>("banner/score_board/hi_score").Text = "0";
            ui.GetNodeByPath<LuiText>("banner/score_board/score_diff").Text =
                "+" + playInfo.Score.ToString("D8").Insert(5, "\'").Insert(2, "\'");
        }

        if (ranking > 0)
        {
            ui.GetNodeByPath<LuiText>("banner/score_board/hi_score").Text =
                (playInfo.Score - 1).ToString("D8").Insert(5, "\'").Insert(2, "\'");
            ui.GetNodeByPath<LuiText>("banner/score_board/score_diff").Text = "+00'000'001";
        }

        //write pure,far,lost etc.
        if (ranking > 0)
        {
            // ranking > 0 for yyw
            ui.GetNodeByPath<LuiText>("banner/perf/pure").Text = playInfo.PerfectCount.ToString();
            ui.GetNodeByPath<LuiText>("banner/perf/big_pure").Text = "+" + playInfo.ShinyPerfectCount;
            ui.GetNodeByPath<LuiText>("banner/perf/far").Text = playInfo.NearCount.ToString();
            ui.GetNodeByPath<LuiText>("banner/perf/lost").Text = playInfo.MissCount.ToString();
            
            ui.GetNodeByPath<LuiText>("banner/perf/big_pure").FontStyle = FontStyle.Regular;
            ui.GetNodeByPath<LuiText>("banner/perf/big_pure").Color = Color.FromArgb(66, 66, 66);

            ui.GetNodeByPath<LuiText>("banner/perf/far_detail").Text = "L0(P0) E0(P0)";
            ui.GetNodeByPath<LuiText>("banner/perf/far_detail").FontStyle = FontStyle.Regular;
            ui.GetNodeByPath<LuiText>("banner/perf/far_detail").Color = Color.FromArgb(66, 66, 66);
        }
        else
        {
            var normalFontColor = Color.FromArgb(32, 32, 32);
            ui.GetNodeByPath<LuiText>("banner/perf/pure").Text = playInfo.PerfectCount.ToString();
            ui.GetNodeByPath<LuiText>("banner/perf/pure").Color = normalFontColor;

            ui.GetNodeByPath<LuiText>("banner/perf/big_pure").Text = "+" + playInfo.ShinyPerfectCount;
            ui.GetNodeByPath<LuiText>("banner/perf/big_pure").Color = normalFontColor;

            ui.GetNodeByPath<LuiText>("banner/perf/far").Text = playInfo.NearCount.ToString();
            ui.GetNodeByPath<LuiText>("banner/perf/far").Color = normalFontColor;

            ui.GetNodeByPath<LuiText>("banner/perf/far_detail").Text =
                $"(P{playInfo.PerfectCount - playInfo.ShinyPerfectCount})";
            ui.GetNodeByPath<LuiText>("banner/perf/far_detail").Color = normalFontColor;

            ui.GetNodeByPath<LuiText>("banner/perf/lost").Text = playInfo.MissCount.ToString();
            ui.GetNodeByPath<LuiText>("banner/perf/lost").Color = normalFontColor;
        }

        var song_info_raw = _songInfoRaw[playInfo.SongId];
        ui.GetNodeByPath<LuiImage>("banner/cover/Image").ImagePath = song_info_raw.GetCover(playInfo.Difficulty);

        var (songNameFont, songNameText) = song_info_raw.GetSongFontAndName(playInfo.Difficulty);
        ui.GetNodeByPath<LuiText>("banner/song_name").Font = songNameFont;
        ui.GetNodeByPath<LuiText>("banner/song_name").Text = songNameText;

        ui.GetNodeByPath<LuiText>("banner/artist_name").Font = song_info_raw.GetArtistFont();
        ui.GetNodeByPath<LuiText>("banner/artist_name").Text = song_info_raw.Artist;

        var rating_f = GetRating(playInfo.SongId, playInfo.Difficulty);

        string rating_str;

        if (rating_f > 0)
            rating_str = rating_f.ToString("F1");
        else
            rating_str = song_info_raw.GetGameRatingStr(playInfo.Difficulty);

        var (diff_str, diff_color) = playInfo.Difficulty switch
        {
            0 => ("Past", Color.Blue),
            1 => ("Present", Color.ForestGreen),
            2 => ("Future", Color.FromArgb(196, 22, 174)),
            3 => ("Beyond", Color.FromArgb(192, 0, 0)),
            _ => ("Unknown", Color.Black)
        };

        ui.GetNodeByPath<LuiText>("banner/diff").Color = diff_color;
        ui.GetNodeByPath<LuiText>("banner/diff").Text = diff_str + " " + rating_str;
        if (ranking == 0)
        {
            ui.GetNodeByPath<LuiText>("banner/max_recall").Text = "PLAY RATING";
            ui.GetNodeByPath<LuiText>("banner/recall").Text = playInfo.Rating.ToString("F3");
        }
        else
        {
            ui.GetNodeByPath<LuiText>("banner/max_recall").Text = "MAX RECALL";
            ui.GetNodeByPath<LuiText>("banner/recall").Text = playInfo.PerfectCount.ToString();
        }

        ui.GetNodeByPath<LuiText>("banner/hp/hp").Text = ((int)playInfo.Health).ToString();

        if (ranking == 0)
        {
            ui.GetNodeByPath<LuiText>("right_btn/Text").Text = "AimuBot";
            ui.GetNodeByPath<LuiText>("right_btn/Text").Font = "Exo";
            ui.GetNodeByPath<LuiText>("right_btn/Text").FontStyle = FontStyle.Bold;
        }

        var target_size_x = ui.Root.Option.CanvasSize.Width * 95 / 100;
        var target_size_y = ui.Root.Option.CanvasSize.Height * 95 / 100;

        if (ranking > 0)
        {
            target_size_x = ui.Root.Option.CanvasSize.Width;
            target_size_y = ui.Root.Option.CanvasSize.Height;
        }

        Image im = new Bitmap(target_size_x, target_size_y);
        var origin = ui.Render();
        using (var g = Graphics.FromImage(im))
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(origin,
                Rectangle.FromLTRB(0, 0, im.Width, im.Height),
                Rectangle.FromLTRB(0, 0, origin.Width, origin.Height),
                GraphicsUnit.Pixel);
        }

        return im;
    }
}