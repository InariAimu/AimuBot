using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea.SlstJson
{
    [JsonObject]
    public class ArcaeaSongDayNightRaw
    {
        [JsonProperty("day")] public string? Day { get; set; }
        [JsonProperty("night")] public string? Night { get; set; }
    }
}
