using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea.AuaJson;

[Serializable]
public class AccountInfo
{
    [JsonProperty("code")] public string? Code { get; set; }
    [JsonProperty("name")] public string? Name { get; set; }
    [JsonProperty("user_id")] public string? UserId { get; set; }
    [JsonProperty("is_mutual")] public bool IsMutual { get; set; }
    [JsonProperty("is_char_uncapped_override")] public bool IsCharUncappedOverride { get; set; }
    [JsonProperty("is_char_uncapped")] public bool IsCharUncapped { get; set; }
    [JsonProperty("is_skill_sealed")] public bool IsSkillSealed { get; set; }
    [JsonProperty("rating")] public int Rating { get; set; }
    [JsonProperty("join_date")] public long JoinDate { get; set; }
    [JsonProperty("character")] public int Character { get; set; }


    [JsonIgnore] public double RealRating => (double)Rating / 100;

    [JsonIgnore]
    public string PttText =>
        RealRating switch
        {
            < 0 => "--",
            _ => RealRating.ToString("F2")
        };
}
