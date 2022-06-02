﻿using System.Reflection;

using AimuBot.Core.Bot;
using AimuBot.Core.Extensions;
using AimuBot.Core.Message;
using AimuBot.Core.Utils;
using AimuBot.Modules;

namespace AimuBot.Core.ModuleMgr;

public class ModuleBase
{
    internal AimuBot? Bot { get; set; }

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
        return attr is not null ? attr.Version : "1.0.0";
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

            var cmdInfo = method.GetCustomAttributes(typeof(CommandAttribute), false);
            
            foreach (var param in cmdInfo)
            {
                if (param.GetType() != typeof(CommandAttribute)) continue;
                
                if (param is not CommandAttribute cmd) continue;
                
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

        _commands = _commands
            .OrderByDescending(x => x.CommandInfo.Command.Length)
            .ThenBy(x => x.CommandInfo.Matching)
            .ToList();

        for (var i = 0; i < _commands.Count; i++)
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
            if (!succ) continue;
            
            var userLevel = AimuBot.Config.RBAC.GetGroupMessageLevel(msg);
            if (userLevel <= cmd.CommandInfo.Level)
            {
                Task.Run(() =>
                {
                    try
                    {
                        msg.Content = body;
                        var result = cmd.InnerMethod?.Invoke(this, new object?[] { msg });
                        var msgChain = result as MessageChain ?? (result as Task<MessageChain>)?.Result;
                        var ret = msgChain?.ToCsCode();
                        BotLogger.LogI(OnGetName(), ret);
                        if (ret == "") return;
                        switch (cmd.CommandInfo.SendType)
                        {
                            case SendType.Send:
                                msg.Bot.SendGroupMessageSimple(msg.SubjectId, ret);
                                break;
                            case SendType.Reply:
                                msg.Bot.ReplyGroupMessageText(msg.SubjectId, msg.Id, ret);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        Information.MessageSent++;
                    }
                    catch (TargetInvocationException ex)
                    {
                        BotLogger.LogE(OnGetName(), nameof(TargetInvocationException));
                        BotLogger.LogE(OnGetName(), $"[{cmd.InnerMethod?.Name}] {ex.InnerException?.Message}\n{ex.InnerException?.StackTrace}");
                    }
                    catch (AggregateException ex)
                    {
                        BotLogger.LogE(OnGetName(), nameof(AggregateException));
                        BotLogger.LogE(OnGetName(), $"[{cmd.InnerMethod?.Name}] {ex.InnerException?.Message}\n{ex.InnerException?.StackTrace}");
                    }
                    catch (Exception ex)
                    {
                        BotLogger.LogE(OnGetName(), $"[{cmd.InnerMethod?.Name}] {ex.Message}\n{ex.StackTrace}");
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

        return false;
    }

    public void LogMessage(object message) => BotLogger.LogI(OnGetName(), message);

    public (bool, string) CheckKeyword(string keyword, BotMessage desc, Matching resolveType)
    {
        var s = desc.Body.Trim();
        switch (resolveType)
        {
            case Matching.Full:
            {
                string? wd = $"/{keyword}";
                if (s.ToLower() == wd)
                    return (true, "");
                break;
            }
            case Matching.StartsWith when keyword == "/":
            {
                string? wd = "/";
                if (s.StartsWith(wd, true))
                    return (true, s.Drop(wd.Length).Trim());
                break;
            }
            case Matching.StartsWith:
            {
                string? wd = $"/{keyword}";
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
