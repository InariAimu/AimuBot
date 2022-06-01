namespace AimuBot.Core.ModuleMgr;

[AttributeUsage(AttributeTargets.Class)]
public class ModuleAttribute : Attribute
{
    public string Name { get; set; }
    public string Version { get; set; } = "1.0.0";
    public string Command { get; set; } = "";
    public string Description { get; set; } = "";
    public string EULA { get; set; } = "";
    public string Privacy { get; set; } = "";

    public ModuleAttribute(string name)
    {
        Name = name;
    }
}
