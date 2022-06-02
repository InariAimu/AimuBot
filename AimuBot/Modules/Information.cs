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
