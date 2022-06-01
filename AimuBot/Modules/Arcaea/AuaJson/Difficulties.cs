
using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea.AuaJson;

[Serializable]
public class Difficulties
{
    [JsonProperty("difficulty")] public int Difficulty { get; set; }
    [JsonProperty("rating")] public int Rating { get; set; }

    [JsonIgnore]
    public float RealRating => Rating / 10f;

    [JsonProperty("note")] public int Note { get; set; }

}
