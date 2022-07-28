using System.Reflection;

using AimuBot.Modules;

namespace AimuBot.Core.ModuleMgr;

public class CommandBase
{
    public ModuleBase MethodModule { get; init; }
    public CommandAttribute CommandInfo { get; init; }
    public MethodInfo InnerMethod { get; init; }
}