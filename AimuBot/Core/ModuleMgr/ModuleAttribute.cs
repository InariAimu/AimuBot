using JetBrains.Annotations;

namespace AimuBot.Core.ModuleMgr;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class ModuleAttribute : Attribute
{
    public string Name { get; set; }
    public string Version { get; set; } = "1.0.0";
    [UsedImplicitly] public string Command { get; set; } = "";
    [UsedImplicitly] public string CommandDesc { get; set; } = "";
    [UsedImplicitly] public string Description { get; set; } = "";
    [UsedImplicitly] public string Eula { get; set; } = "";
    [UsedImplicitly] public string Privacy { get; set; } = "";

    public ModuleAttribute(string name)
    {
        Name = name;
    }
}