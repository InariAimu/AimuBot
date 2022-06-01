using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea.AuaJson;

[Serializable]
public class Content
{
    [JsonProperty("best30_avg")] public double Best30Avg { get; set; }
    [JsonProperty("recent10_avg")] public double Recent10Avg { get; set; }
    [JsonProperty("account_info")] public AccountInfo? AccountInfo { get; set; }
    [JsonProperty("best30_list")] public List<PlayRecord>? Best30List { get; set; }
    [JsonProperty("record")] public PlayRecord? Record { get; set; }
    [JsonProperty("recent_score")] public List<PlayRecord>? RecentScore { get; set; }
    [JsonProperty("best30_overflow")] public List<PlayRecord>? Best30Overflow { get; set; }
    [JsonProperty("song_id")] public string SongId { get; set; }
    [JsonProperty("difficulties")] public List<Difficulties>? Difficulties { get; set; }
}
