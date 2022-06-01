using Newtonsoft.Json;

namespace AimuBot.Core.Bot;

[Serializable]
public class BotConfig
{
    public const string ConfigFileName = "config.json";

    [JsonProperty("bot_name")] public string BotName { get; init; } = "AimuBot";
    [JsonProperty("res_path")] public string ResourcePath { get; init; } = "";

    [JsonProperty("enable_send_message")] public bool EnableSendMessage { get; set; } = true;

    [JsonIgnore]
    public RBAC RBAC { get; private set; } = new RBAC();

}
