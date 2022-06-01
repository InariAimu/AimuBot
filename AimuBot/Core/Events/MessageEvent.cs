using AimuBot.Core.Message;

namespace AimuBot.Core.Events;

public class MessageEvent : BaseEvent
{
    public string Tag { get; }

    public LogLevel Level { get; }

    public BotMessage Message { get; set; }

    private MessageEvent(string tag,
        LogLevel level, string content, BotMessage message)
    {
        Tag = tag;
        Level = level;
        EventMessage = content;
        Message = message;
    }

    internal static MessageEvent Create(string tag, LogLevel level, string content, BotMessage message)
        => new(tag, level, content, message);
}
