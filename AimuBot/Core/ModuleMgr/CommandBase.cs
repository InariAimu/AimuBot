using System.Reflection;
using AimuBot.Modules;

namespace AimuBot.Core.ModuleMgr;

public class CommandBase
{
    public CommandAttribute CommandInfo { get; set; }
    public MethodInfo? InnerMethod { get; set; }
}
