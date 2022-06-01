using System.Reflection;

using AimuBot.Core.Bot;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.Utils;
using AimuBot.Modules;

namespace AimuBot.Core.ModuleMgr;

public class ModuleBase
{
    internal AimuBot? Bot { get; set; } = null;

    private List<CommandBase> _commands = new();

    public string OnGetName()
    {
        var attr = GetType().GetCustomAttribute<ModuleAttribute>();
        if (attr is not null)
        {
            return attr.Name;
        }
        return GetType().FullName ?? "Unknown Module";
    }

    public string OnGetVersion()
    {
        var attr = GetType().GetCustomAttribute<ModuleAttribute>();
        if (attr is not null)
        {
            return attr.Version;
        }
        return "1.0.0";
    }

    public virtual bool OnInit() => OnReload();
    public virtual bool OnReload() => true;
    public virtual bool OnHelp(BotMessage message) => false;
    public virtual bool OnGroupMessage(BotMessage message) => false;

    public int CommandCount => _commands.Count;

    protected T? GetConfig<T>(string key, T? defaultValue = default)
    {
        if (Bot.ModuleMgr.ModuleConfig.TryGet<T>(GetType().FullName, key, out var config))
            return config;

        Bot.ModuleMgr.ModuleConfig.Store(GetType().FullName, key, defaultValue);
        return defaultValue;
    }

    public void LoadCmd()
    {
        var t = GetType();
        var methods = t.GetMethods();

        foreach (var method in methods)
        {
            if (method.ReturnType == typeof(void))
                continue;

            object[]? cmdInfo = method.GetCustomAttributes(typeof(CommandAttribute), false);
            if (cmdInfo != null)
            {
                foreach (object? param in cmdInfo)
                {
                    if (param.GetType() == typeof(CommandAttribute))
                    {
                        if (param is CommandAttribute cmd)
                        {
                            if (cmd.State == State.Normal)
                            {
                                if (method.ReturnType == typeof(MessageChain) || method.ReturnType == typeof(Task<MessageChain>))
                                {
                                    CommandBase cmdFuncBase = new()
                                    {
                                        CommandInfo = cmd,
                                        InnerMethod = method
                                    };
                                    _commands.Add(cmdFuncBase);
                                    BotLogger.LogV(nameof(LoadCmd), $"{t}.{method.Name} => {cmd.Name} {cmd.ShowTip} ");
                                }
                                else
                                {
                                    BotLogger.LogE(nameof(LoadCmd), $"{t}.{method.Name} => [Func mismatch] {cmd.Name} {cmd.ShowTip} ");
                                }
                            }
                            else
                            {
                                BotLogger.LogW(nameof(LoadCmd), $"{t}.{method.Name} => [{cmd.State}] {cmd.Name} {cmd.ShowTip} ");
                            }
                        }
                    }
                }
            }
        }

        _commands = _commands
            .OrderByDescending(x => x.CommandInfo.Command.Length)
            .OrderBy(x => x.CommandInfo.Matching)
            .ToList();

        for (int i = 0; i < _commands.Count; i++)
        {
            var c = _commands[i];
            BotLogger.LogI(nameof(LoadCmd), $"{t} {i:D2} {c.CommandInfo.Matching} {c.CommandInfo.ShowTip}");
        }

        BotLogger.LogI(nameof(LoadCmd), $"{_commands.Count} commands loaded.");
    }

    public bool InternalDealActions(BotMessage msg)
    {
        foreach (var cmd in _commands)
        {
            var (succ, body) = CheckKeyword(cmd.CommandInfo.Command, msg, cmd.CommandInfo.Matching);
            if (succ)
            {
                var userLevel = AimuBot.Config.RBAC.GetGroupMessageLevel(msg);
                if (userLevel <= cmd.CommandInfo.Level)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            msg.Content = body;
                            object? result = cmd.InnerMethod.Invoke(this, new[] { msg });
                            var msgChain = result as MessageChain ?? (result as Task<MessageChain>)?.Result;
                            string? ret = "";
                            ret = msgChain?.ToCsCode();
                            BotLogger.LogI(OnGetName(), ret);
                            if (ret != "")
                            {
                                if (cmd.CommandInfo.SendType == SendType.Send)
                                    msg.Bot.SendGroupMessageSimple(msg.SubjectId, ret);
                                else if (cmd.CommandInfo.SendType == SendType.Reply)
                                    msg.Bot.ReplyGroupMessageText(msg.SubjectId, msg.Id, ret);

                                Information.MessageSent++;
                            }
                        }
                        catch (TargetInvocationException ex)
                        {
                            BotLogger.LogE(OnGetName(), nameof(TargetInvocationException));
                            BotLogger.LogE(OnGetName(), $"[{cmd.InnerMethod.Name}] {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                        }
                        catch (AggregateException ex)
                        {
                            BotLogger.LogE(OnGetName(), nameof(AggregateException));
                            BotLogger.LogE(OnGetName(), $"[{cmd.InnerMethod.Name}] {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                        }
                        catch (Exception ex)
                        {
                            BotLogger.LogE(OnGetName(), $"[{cmd.InnerMethod.Name}] {ex.Message}\n{ex.StackTrace}");
                        }
                    });
                    return true;
                }
                else
                {
                    LogMessage($"[{msg.SubjectName}][{msg.SenderName}] 权限不足 ({userLevel}, {cmd.CommandInfo.Level} required)");
                    if (userLevel != RBACLevel.RestrictedUser)
                        msg.Bot.ReplyGroupMessageText(msg.SubjectId, msg.Id, $"权限不足 ({userLevel}, {cmd.CommandInfo.Level} required)");

                    return false;
                }
            }
        }

        return false;
    }

    public void LogMessage(object message) => BotLogger.LogI(OnGetName(), message);

    public (bool, string) CheckKeyword(string keyword, BotMessage desc, Matching resolveType)
    {
        string? s = desc.Body.Trim();
        if (resolveType == Matching.Full)
        {
            string? wd = $"/{keyword}";
            if (s.ToLower() == wd)
                return (true, "");
        }
        else if (resolveType == Matching.StartsWith)
        {
            if (keyword == "/")
            {
                string? wd = "/";
                if (s.StartsWith(wd, true))
                    return (true, s.Drop(wd.Length).Trim());
            }
            else
            {
                string? wd = $"/{keyword}";
                if (s.StartsWith(wd, true))
                    return (true, s.Drop(wd.Length).Trim());

            }
        }
        else
        if (resolveType == Matching.AnyWithLeadChar)
        {
            if (s.Length >= 1 && s[0] == '/')
            {
                return (true, s[1..]);
            }
        }
        return (false, "");
    }
}
