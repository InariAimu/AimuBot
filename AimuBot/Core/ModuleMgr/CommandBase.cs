using System.Reflection;

using AimuBot.Modules;

namespace AimuBot.Core.ModuleMgr;

public class CommandBase
{
    public CommandBase(CommandAttribute commandInfo, MethodInfo innerMethod)
    {
        CommandInfo = commandInfo;
        InnerMethod = innerMethod;
    }

    public CommandAttribute CommandInfo { get; init; }
    public MethodInfo InnerMethod { get; init; }
}