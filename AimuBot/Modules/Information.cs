using System.Diagnostics;
using System.Management;
using System.Reflection;
using System.Text;

using AimuBot.Core;
using AimuBot.Core.Config;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;
using AimuBot.Core.Utils;

namespace AimuBot.Modules;

[Module("信息",
    Version = "1.0.0",
    Description = "Bot信息模块")]
internal class Information : ModuleBase
{
    private long _startTime = 0;

    public static int MessageReceived { get; set; } = 0;
    public static int MessageProcessed { get; set; } = 0;
    public static int MessageSent { get; set; } = 0;

    public override bool OnInit()
    {
        _startTime = Environment.TickCount64;
        return true;
    }

    [Command("ping",
        Name = "Ping！",
        Template = "/ping",
        Description = "检查 bot 在线状态。回复可能是 Konata, Kagami, Tsukasa, Miyuki 中的任意一个。",        
        NekoBoxExample = 
            "{ position: 'right', msg: '/ping' },"+
            "{ position: 'left', msg: 'Hello, I\\'m Kagami' },",
        Matching = Matching.Exact,
        SendType = SendType.Send)]
    public async Task<MessageChain> OnPing(BotMessage msg)
    {
        await Task.Delay(BotUtil.Random.Next(100, 1000));
        var names = new[] { "Konata", "Kagami", "Tsukasa", "Miyuki" };
        return "Hello, I'm " + names.Random();
    }

    [Command("test-net",
        Name = "网络检查",
        Template = "/test-net",
        Description = "检查bot网络状况:\n- google\n- google proxy\n- github\n- github proxy",
        Matching = Matching.Exact,
        Level = RbacLevel.Super,
        SendType = SendType.Send)]
    public MessageChain OnTestNet(BotMessage msg)
    {
        var urls = new[] { "https://www.google.com.hk", "https://github.com/InariAimu/AimuBot.git" };

        var tasks = new List<Task>();
        foreach (var url in urls)
        {
            tasks.Add(url.UrlDownload(false));
            tasks.Add(url.UrlDownload(true));
        }

        var ret = "";

        try
        {
            Task.WaitAll(tasks.ToArray());
        }
        catch (AggregateException ex)
        {
            foreach (var e in ex.InnerExceptions) BotLogger.LogW(nameof(OnTestNet), ex.Message);
        }
        finally
        {
            ret = string.Join('\n', tasks.Select(t => $"t{t.Id}: {t.Status}"));
        }

        return ret;
    }

    [Command("cs stat",
        Name = "Bot数据",
        Description = "显示 Bot 运行数据",
        Template = "/cs stat",
        Matching = Matching.Exact,
        Level = RbacLevel.Normal)]
    public MessageChain OnStat(BotMessage msg)
    {
        StringBuilder sb = new();
        sb.Append(Bot.Config.BotName + "_CS [Server]");
        sb.Append(" ver." + Assembly.GetExecutingAssembly().GetName().Version?.ToString(3));
        sb.AppendLine();
        sb.Append(".Net " + Environment.Version);
        sb.Append(" " + (Environment.Is64BitProcess ? "x64" : "x86"));
        sb.AppendLine();
        sb.AppendLine();

        var upTime = (Environment.TickCount64 - _startTime) / 1000;
        var ts = TimeSpan.FromSeconds(upTime);
        sb.Append("运行时间：" + ts);
        sb.AppendLine();

        sb.Append($"消息处理：{MessageSent}/{MessageProcessed}/{MessageReceived}");
        sb.AppendLine();

        var currentProcess = Process.GetCurrentProcess();
        var memory = currentProcess.PrivateMemorySize64 / 1048576;
        var workingSet = Environment.WorkingSet / 1048576;
        var physicalMemory = GetPhysicalMemory() / 1048576;
        var percent = (double)workingSet * 100 / physicalMemory;
        sb.Append($"内存使用：{memory}/{workingSet}/{physicalMemory}M ({percent:F2}%)");
        sb.AppendLine();
        sb.AppendLine();

        sb.AppendLine("Adapters:");
        Bot.Instance.BotAdapters.ForEach(x =>
        {
            sb.AppendLine($"[{x.Name}] {x.Status} msg: {x.MessageReceived}/{x.MessageSent}");
        });

        BotLogger.LogI(nameof(OnStat), sb.ToString());

        return sb.ToString().Trim();
    }

    private long GetPhysicalMemory()
    {
        ManagementObjectSearcher searcher = new();
        searcher.Query = new SelectQuery("Win32_PhysicalMemory", "", new string[] { "Capacity" });
        var collection = searcher.Get();
        var em = collection.GetEnumerator();

        long capacity = 0;
        while (em.MoveNext())
        {
            var baseObj = em.Current;
            if (baseObj.Properties["Capacity"].Value == null) continue;
            try
            {
                capacity += long.Parse(baseObj.Properties["Capacity"].Value.ToString() ?? "0");
            }
            catch
            {
                return 0;
            }
        }

        return capacity;
    }
}