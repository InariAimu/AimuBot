namespace AimuBot.Adapters.Connection;

using Newtonsoft.Json;

[Serializable]
public class SocketMessage
{
    [JsonProperty("type")] public string Type { get; set; } = "connection";
    [JsonProperty("conn")] public string Conn { get; set; } = "socket";
    [JsonProperty("protocol")] public string Protocol { get; set; } = "cs";
    [JsonProperty("botadapter")] public string BotAdapter { get; set; } = "mirai";
}