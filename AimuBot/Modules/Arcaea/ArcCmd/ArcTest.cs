using AimuBot.Core.Bot;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;
using AimuBot.Modules.Arcaea.AuaJson;

using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea : ModuleBase
{
    [Command("arc test",
        Name = "test",
        Tip = "/arc test",
        Level = RbacLevel.Super,
        Matching = Matching.Full,
        SendType = SendType.Send)]
    public MessageChain OnArcTest(BotMessage msg)
    {
        Parallel.ForEach(_songInfoRaw.Songs.SongList, async song =>
        {
            var json = await GetFromBotArcApi($"song/info?songid={song.Id}");
            var r = JsonConvert.DeserializeObject<Response>(json);
            if (r is not { Content: { } }) return;
            for (var i = 0; i < r.Content.Difficulties.Count; i++)
            {
                var diff = r.Content.Difficulties[i];
                SongExtra songExtra = new()
                {
                    SongId = song.Id,
                    Difficulty = i,
                    Notes = diff.Note,
                    Rating = diff.Rating
                };
                _db.SaveObject(songExtra);
            }
        });
        return $"Arctest: {_songInfoRaw.Songs.SongList.Sum(x => x.Difficulties.Count)}";
    }

    public void GetAllSongAliasFromBotArcApi()
    {
        foreach (var s in _songInfoRaw.Songs.SongList)
        {
            if (_arcaeaNameAlias.IsNameExist(s.Id))
                continue;

            var sr = GetFromBotArcApi($"song/alias?songid={s.Id}");
            sr.Wait();

            var json = sr.Result;

            Console.Write(s.Id);

            var r = JsonConvert.DeserializeObject<Alias>(json);

            if (r is { Content: { } })
            {
                Console.Write(" " + r.Content.Count);
                r.Content.ForEach(x => _arcaeaNameAlias.SaveNameAlias(s.Id, x));
            }

            Console.WriteLine();

            Thread.Sleep(500);
        }
    }
}