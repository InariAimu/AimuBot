using System.Net;
using System.Text;

using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;
using AimuBot.Modules.Arcaea.AuaJson;

using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea : ModuleBase
{
    [Config("aua_ua")] private readonly string _auaUA = null!;

    [Config("aua_url")] private readonly string _auaUrl = null!;

    [Config("pua_ua")] private readonly string _puaUA = null!;

    [Config("pua_url")] private readonly string _puaUrl = null!;

    public async Task<string> GetFromPyBotArcApi(string request)
    {
        ServicePointManager.ServerCertificateValidationCallback =
            (obj, cert, chain, errs) => true;

        var bytes = await (_puaUrl + request).UrlDownload(true, new Dictionary<string, string>
        {
            { "User-Agent", _puaUA }
        });

        return Encoding.UTF8.GetString(bytes);
    }

    private async Task<string> GetFromBotArcApi(string request)
    {
        ServicePointManager.ServerCertificateValidationCallback =
            (obj, cert, chain, errs) => true;

        LogMessage($"[Aua query] {request}");

        var bytes = await (_auaUrl + request).UrlDownload(true, new Dictionary<string, string>
        {
            { "User-Agent", _auaUA }
        });

        return Encoding.UTF8.GetString(bytes);
    }

    private async Task<Response?> GetUserRecent(string userNameOrCode)
    {
        var json = await GetFromBotArcApi("user/info?user=" + userNameOrCode);

        var r = JsonConvert.DeserializeObject<Response>(json);

        LogMessage("[ArcRecent] ReturnCode:" + (r == null ? "ParseFail" : r.Status.ToString()));

        return r;
    }

    private async Task<Response?> GetUserBest(string userNameOrCode, string songName, string difficulty)
    {
        var json =
            await GetFromBotArcApi(
                $"user/best?usercode={userNameOrCode}&songname={WebUtility.UrlEncode(songName)}&difficulty={difficulty}");

        var r = JsonConvert.DeserializeObject<Response>(json);

        LogMessage("[ArcInfo] ReturnCode:" + (r == null ? "ParseFail" : r.Status.ToString()));

        return r;
    }

    public async Task<Response?> GetB30ResponseFromAua(string userCode)
    {
        LogMessage("start b30 query: " + userCode);

        var json = await GetFromBotArcApi("user/best30?user=" + userCode);

        return JsonConvert.DeserializeObject<Response>(json);
    }

    public async Task<Response?> GetB40ResponseFromAua(string userCode)
    {
        LogMessage("start b40 query: " + userCode);

        var json = await GetFromBotArcApi($"user/best30?user={userCode}&overflow=9");

        return JsonConvert.DeserializeObject<Response>(json);
    }
}