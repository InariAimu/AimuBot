namespace AimuBot.Core.Events;

public class LogEvent : BaseEvent
{
    public string Tag { get; }

    public LogLevel Level { get; }

    private LogEvent(string tag,
        LogLevel level, string content)
    {
        Tag = tag;
        Level = level;
        EventMessage = content;
    }

    internal static LogEvent Create(string tag, LogLevel level, string content)
        => new(tag, level, content);
}