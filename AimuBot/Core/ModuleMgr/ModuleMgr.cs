using System.Reflection;

using AimuBot.Core.Message;
using AimuBot.Core.Utils;
using AimuBot.Modules;

using Newtonsoft.Json;

namespace AimuBot.Core.ModuleMgr;

public class ModuleMgr
{
    private readonly List<ModuleBase> _modules = new();

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
        foreach (var type in types)
        {
            if (!type.IsClass || type.BaseType != typeof(ModuleBase)) continue;
            BotLogger.LogI("LoadModule", $"{type} {type.Name}");

            if (Activator.CreateInstance(type) is not ModuleBase module)
                continue;

            try
            {
                module.LoadCmd();
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

        BotLogger.LogI($"{nameof(ModuleMgr)}.{nameof(Init)}",
            $"{_modules.Count} modules, {_modules.Sum(x => x.CommandCount)} commands Loaded.");
    }

    public bool DispatchGroupMessage(BotMessage messageDesc)
    {
        Information.MessageReceived++;

        var rev = false;

        foreach (var module in _modules)
        {
            if (module.InternalDealActions(messageDesc))
            {
                rev = true;
                break;
            }

            if (module.OnGroupMessage(messageDesc))
            {
                rev = true;
                break;
            }
        }

        //rev = dynamic_inst.OnRunCode(messageDesc);

        if (rev)
            Information.MessageProcessed++;

        return rev;
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