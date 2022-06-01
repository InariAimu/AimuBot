using System.Diagnostics;
using System.Management;
using System.Reflection;
using System.Text;

using AimuBot.Core.Bot;
using AimuBot.Core.Message;
using AimuBot.Core.ModuleMgr;

namespace AimuBot.Modules;

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

    [Command("cs stat",
        Matching = Matching.Full,
        Level = RBACLevel.Normal)]
    public MessageChain OnStat(BotMessage msg)
    {
        StringBuilder sb = new();
        sb.Append(Core.AimuBot.Config.BotName + "_CS [Server]");
        sb.Append(" ver." + Assembly.GetExecutingAssembly().GetName().Version?.ToString(3));
        sb.AppendLine();
        sb.Append(".Net " + Environment.Version.ToString());
        sb.Append(" " + (Environment.Is64BitProcess ? "x64" : "x86"));
        sb.AppendLine();
        sb.AppendLine();

        long up_time = (Environment.TickCount64 - _startTime) / 1000;
        TimeSpan ts = TimeSpan.FromSeconds(up_time);
        sb.Append("运行时间：" + ts.ToString());
        sb.AppendLine();

        sb.Append($"消息处理：{MessageSent}/{MessageProcessed}/{MessageReceived}");
        sb.AppendLine();

        Process proc = Process.GetCurrentProcess();
        long mem_mb = proc.PrivateMemorySize64 / 1048576;
        long ws_mb = Environment.WorkingSet / 1048576;
        long pmem_mb = GetPhysicalMemory() / 1048576;
        double percent = (double)ws_mb * 100 / pmem_mb;
        sb.Append($"内存使用：{mem_mb}/{ws_mb}/{pmem_mb}M ({percent:F2}%)");
        sb.AppendLine();
        //sb.AppendLine();

        //sb.Append("");
        //sb.AppendLine();

        return sb.ToString().Trim();
    }

    public long GetPhysicalMemory()
    {
        ManagementObjectSearcher searcher = new();
        searcher.Query = new SelectQuery("Win32_PhysicalMemory", "", new string[] { "Capacity" });
        var collection = searcher.Get();
        var em = collection.GetEnumerator();

        long capacity = 0;
        while (em.MoveNext())
        {
            var baseObj = em.Current;
            if (baseObj.Properties["Capacity"].Value != null)
            {
                try
                {
                    capacity += long.Parse(baseObj.Properties["Capacity"].Value.ToString());
                }
                catch
                {
                    return 0;
                }
            }
        }
        return capacity;
    }
}
