using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea.SlstJson
{
    [JsonObject]
    public class ArcaeaSongDifficultyRaw
    {
        [JsonProperty("ratingClass")] public int RatingClass { get; set; } = 0;
        [JsonProperty("chartDesigner")] public string? ChartDesigner { get; set; }
        [JsonProperty("jacketDesigner")] public string? JacketDesigner { get; set; }
        [JsonProperty("jacketOverride")] public bool JacketOverride { get; set; } = false;
        [JsonProperty("rating")] public int Rating { get; set; } = 1;
        [JsonProperty("ratingPlus")] public bool RatingPlus { get; set; } = false;
        [JsonProperty("hidden_until_unlocked")] public bool HiddenUntilUnlocked { get; set; } = false;
        [JsonProperty("title_localized")] public ArcaeaSongLocalizeRaw? TitleLocalized { get; set; }
        [JsonProperty("audioOverride")] public bool AudioOverride { get; set; } = false;
        [JsonProperty("plusFingers")] public bool PlusFingers { get; set; } = false;
        [JsonProperty("bg")] public string? Bg { get; set; }
        [JsonProperty("jacket_night")] public string? JacketNight { get; set; }

    }
}
