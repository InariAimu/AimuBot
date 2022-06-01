using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea.AuaJson;

[Serializable]
public class Response
{
    [JsonProperty("status")] public int Status { get; set; }
    [JsonProperty("content")] public Content? Content { get; set; }
    [JsonProperty("message")] public string? Message { get; set; }
}
