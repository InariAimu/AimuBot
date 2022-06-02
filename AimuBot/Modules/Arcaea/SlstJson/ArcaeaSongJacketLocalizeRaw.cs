using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea.SlstJson;

[JsonObject]
public class ArcaeaSongJacketLocalizeRaw
{
    [JsonProperty("en")] public bool En { get; set; } = false;
    [JsonProperty("ja")] public bool Ja { get; set; } = false;
    [JsonProperty("ko")] public bool Ko { get; set; } = false;
    [JsonProperty("kr")] public bool Kr { get; set; } = false;
    [JsonProperty("zh-Hant")] public bool ZhHant { get; set; } = false;
    [JsonProperty("zh-Hans")] public bool ZhHans { get; set; } = false;
}