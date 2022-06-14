using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea.AuaJson;

[Serializable]
public class PlayDataResponse
{
    [JsonProperty("status")] public int Status { get; set; }
    [JsonProperty("content")] public List<ScoreDistItem>? Content { get; set; }
}