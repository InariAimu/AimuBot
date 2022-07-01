using System.Drawing;

using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.Message.Model;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;

using LunaUI.Layouts;

#pragma warning disable CA1416
#pragma warning disable CS8602

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea : ModuleBase
{
    [Command("ac rating",
        Name = "查询定数",
        Description = "查询定数",
        Tip = "/ac rating <rating>",
        Example = "/ac rating 9.9",
        Category = "Arcaea",
        Matching = Matching.StartsWith,
        SendType = SendType.Reply)]
    public MessageChain OnRating(BotMessage msg)
    {
        if (msg.Content.IsNullOrEmpty())
            return "";

        if (!float.TryParse(msg.Content, out var rating))
            return "";

        if (rating < 0)
            return "";

        rating *= 10;

        var rt = _db.GetObjects<SongExtra>("order by rating desc limit 0,1");

        if (rt.Length <= 0)
            return "";

        if (rating > rt[0].Rating)
            rating = rt[0].Rating;

        try
        {
            var im = GetRatingImg((int)rating);
            im?.SaveToJpg(BotUtil.CombinePath($"Arcaea/rating.jpg"), 95);
            return new MessageBuilder(ImageChain.Create($"Arcaea/rating.jpg")).Build();
        }
        catch (Exception ex)
        {
            BotLogger.LogE(nameof(OnRating), $"{ex.Message}/{ex.StackTrace}");
        }

        return "";
    }

    private Color GetDifficultyColor(int difficulty)
        => difficulty switch
        {
            0 => Color.CornflowerBlue,
            1 => Color.LimeGreen,
            2 => Color.Purple,
            3 => Color.Red,
            _ => Color.Black,
        };

    private Image GetRatingImg(int rating)
    {
        LunaUI.LunaUI ui = new(Core.Bot.Config.ResourcePath, "Arcaea/ui/song_rating.json");

        var ratingMinus = rating;
        var ratingPlus = rating;
        var cards = 3;

        if (rating < 80)
        {
            var d = rating % 10;
            if (d < 5)
                rating = (rating / 10) * 10 + 5;
            else if (d < 10)
                rating = (rating / 10) * 10 + 10;
        }

        switch (rating)
        {
            case 80:
                ratingMinus = 75;
                ratingPlus = 81;
                break;
            case <= 75:
                ratingMinus = rating - 5;
                ratingMinus = rating + 5;
                break;
            case > 80:
                ratingMinus = rating - 1;
                ratingPlus = rating + 1;
                break;
        }

        if (ratingMinus < 1) ratingMinus = 1;

        var rt = _db.GetObjects<SongExtra>("order by rating desc limit 0,1");
        var maxRating = rt[0].Rating;

        if (rating == maxRating)
            ratingPlus = -1;
        if (rating == 1)
            ratingMinus = -1;

        var songExtraPlus = _db.GetObjects<SongExtra>("rating=$rating", new() { { "rating", ratingPlus } });
        var songExtra = _db.GetObjects<SongExtra>("rating=$rating", new() { { "rating", rating } });
        var songExtraMinus = _db.GetObjects<SongExtra>("rating=$rating", new() { { "rating", ratingMinus } });

        LogMessage($"{ratingPlus},{rating},{ratingMinus}");

        if (songExtraPlus.Length <= 0)
            cards--;

        if (songExtraMinus.Length <= 0)
            cards--;

        ui.GetNodeByPath<LuiCloneTableLayout>("Image/rating_table").CloneLayouts(cards, "line_");

        var index = 0;
        if (ratingPlus > 0)
        {
            ui.GetNodeByPath<LuiText>($"Image/rating_table/line_{index}/rating").Text =
                $"{ratingPlus / 10}.{ratingPlus % 10}";

            ui.GetNodeByPath<LuiCloneTableLayout>($"Image/rating_table/line_{index}/song_table")
                .CloneLayouts(songExtraPlus.Length, "song_");

            var i = 0;
            foreach (var song in songExtraPlus)
            {
                var s = _songInfoRaw.Songs.SongList.Find(x => x.Id == song.SongId);
                ui.GetNodeByPath<LuiImage>($"Image/rating_table/line_{index}/song_table/song_{i}").ImagePath =
                    s.GetCover(song.Difficulty);
                
                if (song.Difficulty == 2)
                    ui.GetNodeByPath<LuiRect>($"Image/rating_table/line_{index}/song_table/song_{i}/diff").Visible =
                        false;
                else
                    ui.GetNodeByPath<LuiRect>($"Image/rating_table/line_{index}/song_table/song_{i}/diff").Color =
                        GetDifficultyColor(song.Difficulty);
                i++;
            }

            index++;
        }

        {
            ui.GetNodeByPath<LuiText>($"Image/rating_table/line_{index}/rating").Text = $"{rating / 10}.{rating % 10}";
            ui.GetNodeByPath<LuiCloneTableLayout>($"Image/rating_table/line_{index}/song_table")
                .CloneLayouts(songExtra.Length, "song_");

            var i = 0;
            foreach (var song in songExtra)
            {
                var s = _songInfoRaw.Songs.SongList.Find(x => x.Id == song.SongId);
                ui.GetNodeByPath<LuiImage>($"Image/rating_table/line_{index}/song_table/song_{i}").ImagePath =
                    s.GetCover(song.Difficulty);
                
                if (song.Difficulty == 2)
                    ui.GetNodeByPath<LuiRect>($"Image/rating_table/line_{index}/song_table/song_{i}/diff").Visible =
                        false;
                else
                    ui.GetNodeByPath<LuiRect>($"Image/rating_table/line_{index}/song_table/song_{i}/diff").Color =
                        GetDifficultyColor(song.Difficulty);
                i++;
            }

            index++;
        }

        if (ratingMinus > 0)
        {
            ui.GetNodeByPath<LuiText>($"Image/rating_table/line_{index}/rating").Text =
                $"{ratingMinus / 10}.{ratingMinus % 10}";

            ui.GetNodeByPath<LuiCloneTableLayout>($"Image/rating_table/line_{index}/song_table")
                .CloneLayouts(songExtraMinus.Length, "song_");

            var i = 0;
            foreach (var song in songExtraMinus)
            {
                var s = _songInfoRaw.Songs.SongList.Find(x => x.Id == song.SongId);
                ui.GetNodeByPath<LuiImage>($"Image/rating_table/line_{index}/song_table/song_{i}").ImagePath =
                    s.GetCover(song.Difficulty);
                
                if (song.Difficulty == 2)
                    ui.GetNodeByPath<LuiRect>($"Image/rating_table/line_{index}/song_table/song_{i}/diff").Visible =
                        false;
                else
                    ui.GetNodeByPath<LuiRect>($"Image/rating_table/line_{index}/song_table/song_{i}/diff").Color =
                        GetDifficultyColor(song.Difficulty);
                i++;
            }
        }

        return ui.Render();
    }
}