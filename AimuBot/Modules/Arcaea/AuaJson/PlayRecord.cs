using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea.AuaJson;

[Serializable]
public class PlayRecord
{
    [JsonProperty("score")] public int Score { get; set; }
    [JsonProperty("health")] public double Health { get; set; }
    [JsonProperty("rating")] public double Rating { get; set; }
    [JsonProperty("song_id")] public string? SongId { get; set; }
    [JsonProperty("modifier")] public double Modifier { get; set; }
    [JsonProperty("difficulty")] public int Difficulty { get; set; }
    [JsonProperty("clear_type")] public double ClearType { get; set; }
    [JsonProperty("best_clear_type")] public double BestClearType { get; set; }
    [JsonProperty("time_played")] public long TimePlayed { get; set; }
    [JsonProperty("near_count")] public int NearCount { get; set; }
    [JsonProperty("miss_count")] public int MissCount { get; set; }
    [JsonProperty("perfect_count")] public int PerfectCount { get; set; }
    [JsonProperty("shiny_perfect_count")] public int ShinyPerfectCount { get; set; }
}