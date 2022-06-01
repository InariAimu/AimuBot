using System.Net;
using System.Net.Security;
using AimuBot.Core.ModuleMgr;
using AimuBot.Modules.Arcaea.AuaJson;

using Newtonsoft.Json;

namespace AimuBot.Modules.Arcaea;

public partial class Arcaea : ModuleBase
{
    [Config("aua_url")]
    private string _auaUrl = null!;

    [Config("aua_ua")]
    private string _auaUA = null!;

    [Config("pua_url")]
    private string _puaUrl = null!;

    [Config("pua_ua")]
    private string _puaUA = null!;

    public async Task<string> GetFromPyBotArcApi(string request)
    {
        string? html = "";
        ServicePointManager.ServerCertificateValidationCallback =
            new RemoteCertificateValidationCallback((obj, cert, chain, errs) => true);
        using (HttpClient hc = new())
        {
            LogMessage($"[PyAua query] {request}");
            hc.DefaultRequestHeaders.Add("User-Agent", _puaUA);
            html = await hc.GetStringAsync(_puaUrl + request);
        }
        return html;
    }

    public async Task<string> GetFromBotArcApi(string request)
    {
        string? html = "";
        ServicePointManager.ServerCertificateValidationCallback =
            new RemoteCertificateValidationCallback((obj, cert, chain, errs) => true);
        using (HttpClient hc = new())
        {
            LogMessage($"[Aua query] {request}");
            hc.DefaultRequestHeaders.Add("User-Agent", _auaUA);
            html = await hc.GetStringAsync(_auaUrl + request);
        }
        return html;
    }

    public async Task<Response?> GetUserRecent(string user_name_or_code)
    {
        string? json = await GetFromBotArcApi("user/info?user=" + user_name_or_code);

        var r = JsonConvert.DeserializeObject<Response>(json);

        LogMessage("[ArcRecent] ReturnCode:" + (r == null ? "ParseFail" : r.Status.ToString()));

        return r;
    }

    public async Task<Response?> GetUserBest(string user_name_or_code, string song_name, string difficulty)
    {
        string? json =
            await GetFromBotArcApi($"user/best?usercode={user_name_or_code}&songname={WebUtility.UrlEncode(song_name)}&difficulty={difficulty}");

        var r = JsonConvert.DeserializeObject<Response>(json);

        LogMessage("[ArcInfo] ReturnCode:" + (r == null ? "ParseFail" : r.Status.ToString()));

        return r;
    }

    public async Task<Response?> GetB30ResponseFromAua(string user_code)
    {
        LogMessage("start b30 query: " + user_code);

        string? json = await GetFromBotArcApi("user/best30?user=" + user_code);

        return JsonConvert.DeserializeObject<Response>(json);
    }
}
