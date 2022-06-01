using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea.SlstJson
{
    [JsonObject]
    public class ArcaeaSongListRaw
    {
        [JsonProperty("songs")] public List<ArcaeaSongRaw>? SongList { get; set; }

    }
}
