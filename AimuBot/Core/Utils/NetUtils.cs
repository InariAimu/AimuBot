using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AimuBot.Core.Utils;

public static class NetUtils
{
    /// <summary>
    /// UrlDownload file
    /// </summary>
    /// <param name="url"></param>
    /// <param name="useProxy"></param>
    /// <param name="header"></param>
    /// <param name="timeout"></param>
    /// <param name="limitLen">default 2 gigabytes</param>
    /// <returns></returns>
    public static async Task<byte[]> UrlDownload(this string url, bool useProxy = false,
        Dictionary<string, string>? header = null,
        int timeout = 8000, long limitLen = ((long)2 << 30) - 1)
    {
        // Create request
        var request = new HttpClient(
            new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                Proxy = new WebProxy()
                {
                    Address = new Uri("http://localhost:7890"),
                    BypassProxyOnLocal = false
                },
                UseProxy = useProxy
            })
        {
            Timeout = new TimeSpan(0, 0, 0, timeout),
            MaxResponseContentBufferSize = limitLen
        };

        // Default useragent
        request.DefaultRequestHeaders.Add("User-Agent", new[]
        {
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
            "AppleWebKit/537.36 (KHTML, like Gecko)",
            "Chrome/100.0.4896.127 Safari/537.36 Edg/100.0.1185.50",
            "Kagami/1.0.0 (Konata Project)"
        });

        // Append request header
        if (header is not null)
            foreach (var (k, v) in header)
                request.DefaultRequestHeaders.Add(k, v);

        // Open response stream
        var response = await request.GetByteArrayAsync(url);

        // Receive the response data
        return response;
    }

    /// <summary>
    /// Get meta data
    /// </summary>
    /// <param name="keys"></param>
    /// <param name="html"></param>
    /// <returns></returns>
    public static Dictionary<string, string> GetMetaData(this string html, params string[] keys)
    {
        var metaDict = new Dictionary<string, string>();

        foreach (var i in keys)
        {
            var pattern = i + @"=""(.*?)""(.|\s)*?content=""(.*?)"".*?>";

            // Match results
            foreach (Match j in Regex.Matches(html, pattern, RegexOptions.Multiline))
                metaDict.TryAdd(j.Groups[1].Value, j.Groups[3].Value);
        }

        return metaDict;
    }
}