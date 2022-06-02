using Newtonsoft.Json;

namespace AimuBot.Core.ModuleMgr;

[AttributeUsage(AttributeTargets.Field)]
public class ConfigAttribute : Attribute
{
    public string Name { get; init; }
    public object? DefaultValue { get; set; } = null;

    public ConfigAttribute(string name)
    {
        Name = name;
    }
}

[Serializable]
public class ModuleConfig
{
    [JsonProperty("module_configs")]
    public Dictionary<string, Dictionary<string, object>> ModuleConfs { get; private set; }
        = new();

    public void Store(string moduleName, string key, object value)
    {
        if (ModuleConfs.TryGetValue(moduleName, out var dict))
            dict.Add(key, value);
        else
            ModuleConfs.Add(moduleName, new Dictionary<string, object>
            {
                { key, value }
            });
    }

    public bool TryGet<T>(string moduleName, string key, out T? value)
    {
        value = default;
        if (!ModuleConfs.TryGetValue(moduleName, out var dict)) return false;
        if (!dict.TryGetValue(key, out var v)) return false;
        value = (T?)v;
        return v is T?;
    }

    public object? Get(string moduleName, string key)
    {
        object? value = default;
        if (!ModuleConfs.TryGetValue(moduleName, out var dict)) return value;
        if (dict.TryGetValue(key, out var v)) value = v;
        return value;
    }
}