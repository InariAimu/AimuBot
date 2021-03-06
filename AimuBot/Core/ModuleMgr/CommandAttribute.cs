using AimuBot.Core.Config;

using JetBrains.Annotations;

namespace AimuBot.Core.ModuleMgr;

public enum Matching
{
    Exact,
    StartsWith,
    StartsWithNoLeadChar,
    Regex,
    AnyWithLeadChar,
    Any
}

public enum SendType
{
    Custom,
    Reply,
    Send
}

public enum State
{
    Normal,
    Test,
    Developing,
    DisableByDefault,
    Disabled,
}

public enum CooldownType
{
    None,
    User,
    Group,
    Bot,
    Global
}

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string Name { get; set; } = "";
    public string Command { get; }
    public string[]? Alias { get; set; }
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string[]? BlocksBefore { get; set; } = null;
    public string Template { get; set; } = "";
    public string Example { get; set; } = "";
    public string NekoBoxExample { get; set; } = "";
    public string DescCustomFunc { get; set; } = "";
    public bool NeedSensitivityCheck { get; set; }
    public CooldownType CooldownType { get; set; } = CooldownType.None;
    public int CooldownSecond { get; set; }
    public State State { get; set; } = State.Normal;
    public Matching Matching { get; set; } = Matching.StartsWith;
    public RbacLevel Level { get; set; } = RbacLevel.Normal;
    public SendType SendType { get; set; } = SendType.Send;

    public CommandAttribute(string cmd)
    {
        Command = cmd;
    }

    /// <summary>
    /// For display purpose. replaced \n to \\n
    /// </summary>
    public string ShowTip => Template.Replace("\n", "\\n");
}