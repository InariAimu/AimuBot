using System.Reflection;

using AimuBot.Core.Config;
using AimuBot.Core.Message;
using AimuBot.Core.Utils;
using AimuBot.Modules;

using Newtonsoft.Json;

namespace AimuBot.Core.ModuleMgr;

public class ModuleMgr
{
    private readonly List<ModuleBase> _modules = new();

    // Commands must be rearranged by it's matching type globally.
    private List<CommandBase> _commands = new();
    public int CommandCount => _commands.Count;
    public List<CommandBase> CommandList => _commands;
    
    public List<ModuleBase> ModuleList => _modules;
    
    public ModuleConfig? ModuleConfig { get; set; }

    public bool LoadSubModulesConfig()
    {
        var path = BotUtil.CombinePath("module_config.json");
        if (File.Exists(path))
        {
            ModuleConfig = JsonConvert.DeserializeObject<ModuleConfig>(File.ReadAllText(path));
            return true;
        }

        ModuleConfig = new ModuleConfig();
        return false;
    }

    public void SaveSubModulesConfig()
    {
        var path = BotUtil.CombinePath("module_config.json");
        if (ModuleConfig is not null)
            File.WriteAllText(path, JsonConvert.SerializeObject(ModuleConfig, Formatting.Indented));
    }

    public void Init()
    {
        var hasConfigFile = LoadSubModulesConfig();

        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types.OrderBy(x => x.Name))
        {
            if (!type.IsClass || type.BaseType != typeof(ModuleBase)) continue;
            BotLogger.LogI("LoadModule", $"{type} {type.Name}");

            if (Activator.CreateInstance(type) is not ModuleBase module)
                continue;

            try
            {
                module.LoadCommands();
                module.OnInit();
                module.OnReload();

                //load module config
                var fields = type.GetFields(
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);

                foreach (var field in fields)
                {
                    var ca = field.GetCustomAttribute<ConfigAttribute>();
                    if (ca is null) continue;

                    var fv = ModuleConfig.Get(type.FullName, ca.Name);
                    if (fv is null)
                    {
                        fv = ca.DefaultValue;
                        ModuleConfig.Store(type.FullName, ca.Name, ca.DefaultValue);
                    }

                    field.SetValue(module, fv);
                }
            }
            catch (Exception ex)
            {
                BotLogger.LogE("LoadModule", $"{ex.Message}\n{ex.StackTrace}");
                continue;
            }

            _modules.Add(module);
        }

        if (!hasConfigFile)
        {
            BotLogger.LogW($"{nameof(ModuleMgr)}.{nameof(Init)}",
                "No config file detected. Generate one.");
            SaveSubModulesConfig();
        }

        _commands = _commands
            .OrderBy(x => x.CommandInfo.Matching)
            .ThenByDescending(x => x.CommandInfo.Command.Length)
            .ToList();

        for (var i = 0; i < _commands.Count; i++)
        {
            var c = _commands[i];
            BotLogger.LogI(nameof(Init),
                $"{c.MethodModule.GetType()} {i:D2} {c.CommandInfo.Matching} {c.CommandInfo.ShowTip}");
        }

        BotLogger.LogI($"{nameof(ModuleMgr)}.{nameof(Init)}",
            $"{_modules.Count} modules, {_commands.Count} commands Loaded.");
    }

    public bool DispatchGroupMessage(BotMessage msg)
    {
        Information.MessageReceived++;

        var rev = false;

        rev = ProcessCommands(msg);

        if (!rev)
        {
            foreach (var module in _modules)
            {
                if (!module.OnGroupMessage(msg)) continue;
                rev = true;
                break;
            }
        }

        //rev = dynamic_inst.OnRunCode(messageDesc);

        if (rev)
            Information.MessageProcessed++;

        return rev;
    }

    private bool ProcessCommands(BotMessage msg)
    {
        foreach (var cmd in _commands)
        {
            var (succ, body) = cmd.MethodModule.CheckKeyword(cmd.CommandInfo.Command, msg, cmd.CommandInfo.Matching);
            if (!succ) continue;

            var userLevel = Bot.Config.AccessLevelControl.GetGroupMessageLevel(msg);
            if (userLevel > RbacLevel.Super &&
                cmd.CommandInfo.State is State.Disabled or State.Developing or State.DisableByDefault)
                continue;

            if (userLevel == RbacLevel.Super || userLevel <= cmd.CommandInfo.Level)
            {
                Task.Run(() =>
                {
                    try
                    {
                        msg.Content = body;
                        if (cmd.CommandInfo.SendType == SendType.Custom)
                        {
                            cmd.InnerMethod?.Invoke(this, new object[] { msg });
                        }
                        else
                        {
                            var result = cmd.InnerMethod?.Invoke(cmd.MethodModule, new object[] { msg });
                            var msgChain = result as MessageChain ?? (result as Task<MessageChain>)?.Result;
                            var ret = msgChain?.ToCsCode();
                            if (ret is null) return;

                            BotLogger.LogI(cmd.MethodModule.OnGetName(), ret);
                            if (ret == "") return;

                            CommandInvokeLogger.Instance.Log(cmd.InnerMethod, msg, msgChain);

                            switch (cmd.CommandInfo.SendType)
                            {
                                case SendType.Send:
                                    msg.Bot.SendGroupMessageSimple(msg.SubjectId, ret);
                                    break;
                                case SendType.Reply:
                                    msg.Bot.ReplyGroupMessageText(msg.SubjectId, msg.Id, ret);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException($"No such type: {cmd.CommandInfo.SendType}");
                            }

                            Information.MessageSent++;
                        }
                    }
                    catch (TargetInvocationException ex)
                    {
                        BotLogger.LogE(cmd.MethodModule.OnGetName(), nameof(TargetInvocationException));
                        BotLogger.LogE(cmd.MethodModule.OnGetName(),
                            $"[{cmd.InnerMethod?.Name}] {ex.InnerException?.Message}\n{ex.InnerException?.StackTrace}");
                    }
                    catch (AggregateException ex)
                    {
                        BotLogger.LogE(cmd.MethodModule.OnGetName(), nameof(AggregateException));
                        BotLogger.LogE(cmd.MethodModule.OnGetName(),
                            $"[{cmd.InnerMethod?.Name}] {ex.InnerException?.Message}\n{ex.InnerException?.StackTrace}");
                    }
                    catch (Exception ex)
                    {
                        BotLogger.LogE(cmd.MethodModule.OnGetName(),
                            $"[{cmd.InnerMethod?.Name}] {ex.Message}\n{ex.StackTrace}");
                    }
                });
                return true;
            }

            BotLogger.LogI($"[{msg.SubjectName}]",
                "[{msg.SenderName}] 权限不足 ({userLevel}, {cmd.CommandInfo.Level} required)");
            /*if (userLevel != RbacLevel.RestrictedUser)
                msg.Bot.ReplyGroupMessageText(msg.SubjectId, msg.Id,
                    $"权限不足 ({userLevel}, {cmd.CommandInfo.Level} required)");
            */
            return false;
        }

        return false;
    }

    public string ReloadModule(string name)
    {
        try
        {
            var m = _modules.Find(x => x.OnGetName() == name);
            if (m != null)
                m.OnReload();
            else
                return "";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }

        return "";
    }
}