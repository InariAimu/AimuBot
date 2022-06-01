using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea.SlstJson
{
    [JsonObject]
    public class ArcaeaSongLocalizeRaw
    {
        [JsonProperty("en")] public string? En { get; set; }
        [JsonProperty("ja")] public string? Ja { get; set; }
        [JsonProperty("ko")] public string? Ko { get; set; }
        [JsonProperty("kr")] public string? Kr { get; set; }
        [JsonProperty("zh-Hant")] public string? ZhHant { get; set; }
        [JsonProperty("zh-Hans")] public string? ZhHans { get; set; }
    }
}
