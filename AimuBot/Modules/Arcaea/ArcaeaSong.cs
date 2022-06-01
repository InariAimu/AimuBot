﻿using AimuBot.Core.Utils;
using AimuBot.Modules.Arcaea.SlstJson;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace AimuBot.Modules.Arcaea;

class ArcaeaSongListRawWrapper
{
    public ArcaeaSongListRaw? Songs { get; private set; }

    public void LoadFromArcaeaAPPSongList()
    {
        string json = File.ReadAllText(
            BotUtil.CombinePath($"arcaea/assets/songs/songlist"));

        DefaultContractResolver contractResolver = new();
        Songs = JsonConvert.DeserializeObject<ArcaeaSongListRaw>(json, new JsonSerializerSettings()
        {
            ContractResolver = contractResolver,
            Formatting = Formatting.None
        });
    }

    public ArcaeaSongRaw? this[string id] => Songs.SongList.Find(x => x.Id == id);
}
