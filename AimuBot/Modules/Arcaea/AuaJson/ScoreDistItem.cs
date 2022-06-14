using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea.AuaJson;

[Serializable]
public class ScoreDistItem
{
    [JsonProperty("fscore")] public int Fscore { get; set; }
    [JsonProperty("count")] public int Count { get; set; }
}