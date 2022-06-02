using System.Text.RegularExpressions;

using AimuBot.Core.Extensions;

using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea.SlstJson;

[JsonObject]
public class ArcaeaSongRaw
{
    [JsonProperty("idx")] public int Idx { get; set; } = 0;
    [JsonProperty("id")] public string Id { get; set; } = "";
    [JsonProperty("title_localized")] public ArcaeaSongLocalizeRaw? TitleLocalized { get; set; }
    [JsonProperty("jacket_localized")] public ArcaeaSongJacketLocalizeRaw? JacketLocalized { get; set; }
    [JsonProperty("artist")] public string Artist { get; set; } = "";
    [JsonProperty("bpm")] public string Bpm { get; set; } = "";
    [JsonProperty("bpm_base")] public double BpmBase { get; set; } = 0f;
    [JsonProperty("set")] public string Set { get; set; } = "";
    [JsonProperty("purchase")] public string Purchase { get; set; } = "";
    [JsonProperty("audioPreview")] public int AudioPreview { get; set; } = 0;
    [JsonProperty("audioPreviewEnd")] public int AudioPreviewEnd { get; set; } = 0;
    [JsonProperty("side")] public int Side { get; set; } = 0;
    [JsonProperty("bg")] public string Bg { get; set; } = "";
    [JsonProperty("bg_daynight")] public ArcaeaSongDayNightRaw? BgDaynight { get; set; }
    [JsonProperty("remote_dl")] public bool RemoteDl { get; set; } = false;
    [JsonProperty("source_localized")] public ArcaeaSongLocalizeRaw? SourceLocalized { get; set; }
    [JsonProperty("source_copyright")] public string SourceCopyright { get; set; } = "";
    [JsonProperty("no_stream")] public bool NoStream { get; set; } = false;
    [JsonProperty("world_unlock")] public bool WorldUnlock { get; set; } = false;
    [JsonProperty("byd_local_unlock")] public bool BydLocalUnlock { get; set; } = false;
    [JsonProperty("songlist_hidden")] public bool SonglistHidden { get; set; } = false;
    [JsonProperty("date")] public int Date { get; set; } = 0;
    [JsonProperty("version")] public string Version { get; set; } = "";
    [JsonProperty("difficulties")] public List<ArcaeaSongDifficultyRaw>? Difficulties { get; set; }

    public string GetArtistFont()
    {
        if (new Regex(@"^[A-Za-z\d\s\.\(\)]+$").IsMatch(Artist))
            return "Noto Sans CJK SC Regular";
        else
            return "Kazesawa Regular";
    }

    public string GetGameRatingStr(int difficulty)
    {
        var diff = Difficulties[difficulty];
        if (diff is null)
            return "";

        return diff.Rating.ToString() + (diff.RatingPlus ? "+" : "");
    }

    public (string, string) GetSongFontAndName(int difficulty)
    {
        var diff = Difficulties[difficulty];
        if (diff.TitleLocalized != null)
        {
            if (diff.TitleLocalized.Ja.IsNullOrEmpty())
                return ("Noto Sans CJK SC Regular", diff.TitleLocalized.En ?? "");
            else
                return ("Kazesawa Regular", diff.TitleLocalized.Ja ?? "");
        }
        else
        {
            if (TitleLocalized.Ja.IsNullOrEmpty())
                return ("Noto Sans CJK SC Regular", TitleLocalized.En ?? "");
            else
                return ("Kazesawa Regular", TitleLocalized.Ja ?? "");
        }
    }

    public bool IsTitleMatch(string keyword)
    {
        if (TitleLocalized is not null)
        {
            if (TitleLocalized.En is not null)
                if (TitleLocalized.En.ToLower() == keyword)
                    return true;

            if (TitleLocalized.Ja is not null)
                if (TitleLocalized.Ja.ToLower() == keyword)
                    return true;
        }

        return false;
    }

    public (string?, string?) GetTitle() => (TitleLocalized.En, TitleLocalized.Ja);

    public string GetPath()
    {
        var dir_name = Id;

        if (RemoteDl)
            dir_name = "dl_" + dir_name;

        return "Arcaea/assets/songs/" + dir_name + "/";
    }

    public string GetBg(int level = 2)
    {
        var bg = "";

        if (Difficulties is not null && Difficulties.Count > level)
        {
            var diff = Difficulties[level];
            if (!diff.Bg.IsNullOrEmpty())
                bg = diff.Bg;
        }

        if (bg.IsNullOrEmpty())
            bg = Bg;

        if (bg.IsNullOrEmpty())
            bg = Side == 0 ? "base_light" : "base_conflict";

        return bg;
    }

    public string GetCover(int level = 2)
    {
        var dir_name = Id;

        if (RemoteDl)
            dir_name = "dl_" + dir_name;

        if (Difficulties is not null && Difficulties.Count > level)
        {
            var diff = Difficulties[level];

            if (!diff.JacketNight.IsNullOrEmpty())
            {
                var dt = DateTime.Now;
                if (dt.Hour >= 20 || dt.Hour < 6)
                    return $"arcaea/assets/songs/{dir_name}/base_night.jpg";
            }

            if (diff.JacketOverride)
                return $"arcaea/assets/songs/{dir_name}/{level}.jpg";
        }

        return $"arcaea/assets/songs/{dir_name}/base.jpg";
    }
}