
using AimuBot.Core.Bot;

namespace AimuBot.Core.ModuleMgr;

public enum Matching
{
    Full,
    StartsWith,
    StartsWithNoLeadChar,
    Regex,
    AnyWithLeadChar,
    Any,
}

public enum SendType
{
    Custom,
    Reply,
    Send,
}

public enum State
{
    Normal,
    Test,
    Developing,
    DisableByDefault,
}

public enum CooldownType
{
    None,
    User,
    Group,
    Bot,
    Global,
}

[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute : Attribute
{
    public string Name { get; set; } = "";
    public string Command { get; } = "";
    public string[]? Alias { get; set; }
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string Tip { get; set; } = "";
    public string Example { get; set; } = "";
    public bool NeedSensitivityCheck { get; set; }
    public CooldownType CooldownType { get; set; } = CooldownType.None;
    public int CooldownSecond { get; set; }
    public State State { get; set; } = State.Normal;
    public Matching Matching { get; set; } = Matching.StartsWith;
    public RBACLevel Level { get; set; } = RBACLevel.Normal;
    public SendType SendType { get; set; } = SendType.Custom;

    public CommandAttribute(string cmd) => Command = cmd;

    /// <summary>
    /// For display purpose. replaced \n to \\n
    /// </summary>
    public string ShowTip => Tip.Replace("\n", "\\n");
}
