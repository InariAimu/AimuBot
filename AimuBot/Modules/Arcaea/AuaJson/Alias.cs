using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea.AuaJson;

[Serializable]
public class Alias
{
    [JsonProperty("status")] public int Status { get; set; }
    [JsonProperty("content")] public List<string>? Content { get; set; }
}