using System.Net.WebSockets;
using System.Reflection;

using AimuBot.Core.Config;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.Utils;
using AimuBot.Modules;

namespace AimuBot.Core.ModuleMgr;

public class ModuleBase
{
    public string OnGetName()
    {
        var attr = GetType().GetCustomAttribute<ModuleAttribute>();
        if (attr is not null) return attr.Name;
        return GetType().FullName ?? "Unknown Module";
    }

    public string OnGetVersion()
    {
        var attr = GetType().GetCustomAttribute<ModuleAttribute>();
        return attr is not null ? attr.Version : "1.0.0";
    }

    public virtual bool OnInit() => OnReload();
    public virtual bool OnReload() => true;
    public virtual bool OnGroupMessage(BotMessage message) => false;
    
    protected T? GetConfig<T>(string key, T? defaultValue = default)
    {
        if (Bot.Instance.ModuleMgr.ModuleConfig.TryGet<T>(GetType().FullName, key, out var config))
            return config;

        Bot.Instance.ModuleMgr.ModuleConfig.Store(GetType().FullName, key, defaultValue);
        return defaultValue;
    }

    public void LoadCommands()
    {
        var t = GetType();
        var methods = t.GetMethods();

        foreach (var method in methods)
        {
            if (method.ReturnType == typeof(void))
                continue;

            var cmdInfo = method.GetCustomAttributes(typeof(CommandAttribute), false);

            foreach (var param in cmdInfo)
            {
                if (param.GetType() != typeof(CommandAttribute)) continue;

                if (param is not CommandAttribute cmd) continue;

                if (cmd.State is State.Normal or State.Test or State.Disabled)
                {
                    if (method.ReturnType == typeof(MessageChain) || method.ReturnType == typeof(Task<MessageChain>))
                    {
                        CommandBase cmdFuncBase = new()
                        {
                            CommandInfo = cmd,
                            InnerMethod = method,
                            MethodModule = this,
                        };
                        Bot.Instance.ModuleMgr.CommandList.Add(cmdFuncBase);
                        if (cmd.State is not State.Normal)
                            BotLogger.LogW(nameof(LoadCommands),
                                $"{t}.{method.Name} => [{cmd.State}] {cmd.Name} {cmd.ShowTip} ");
                        else
                            BotLogger.LogV(nameof(LoadCommands), $"{t}.{method.Name} => {cmd.Name} {cmd.ShowTip} ");
                    }
                    else
                    {
                        BotLogger.LogE(nameof(LoadCommands),
                            $"{t}.{method.Name} => [Func mismatch] {cmd.Name} {cmd.ShowTip} ");
                    }
                }
                else
                {
                    BotLogger.LogW(nameof(LoadCommands), $"{t}.{method.Name} => [{cmd.State}] {cmd.Name} {cmd.ShowTip} ");
                }
            }
        }

    }

    public void LogMessage(object message) => BotLogger.LogI(OnGetName(), message);

    public (bool, string) CheckKeyword(string keyword, BotMessage desc, Matching resolveType)
    {
        var s = desc.Body.Trim();
        switch (resolveType)
        {
            case Matching.Exact:
            {
                var wd = $"/{keyword}";
                if (s.ToLower() == wd)
                    return (true, "");
                break;
            }
            case Matching.StartsWith:
            {
                var wd = $"/{keyword}";
                if (s.StartsWith(wd, true))
                    return (true, s.Drop(wd.Length).Trim());
                break;
            }
            case Matching.StartsWithNoLeadChar when s.StartsWith(keyword, true):
                return (true, s.Drop(keyword.Length).Trim());

            case Matching.AnyWithLeadChar when s.Length >= 1 && s[0] == '/':
                return (true, s[1..]);
        }

        return (false, "");
    }
}