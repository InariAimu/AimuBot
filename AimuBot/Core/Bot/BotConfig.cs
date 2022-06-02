using Newtonsoft.Json;

namespace AimuBot.Core.Bot;

[Serializable]
public class BotConfig
{
    public const string ConfigFileName = "config.json";

    [JsonProperty("bot_name")] public string BotName { get; init; } = "AimuBot";
    [JsonProperty("res_path")] public string ResourcePath { get; init; } = "";

    [JsonProperty("enable_send_message")] public bool EnableSendMessage { get; set; } = true;

    [JsonProperty("use_proxy")] public bool UseProxy { get; set; } = true;
    [JsonProperty("proxy_url")] public string ProxyUrl { get; set; } = "http://localhost:7890";

    [JsonIgnore] public AccessLevelControl AccessLevelControl { get; private set; } = new();
}